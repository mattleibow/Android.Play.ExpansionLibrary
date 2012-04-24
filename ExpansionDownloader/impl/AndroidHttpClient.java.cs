/**
 * Subclass of the Apache {@link DefaultHttpClient} that is configured with
 * reasonable default settings and registered schemes for Android, and
 * also lets the user add {@link HttpRequestInterceptor} classes.
 * Don't create this directly, use the {@link #newInstance} factory method.
 *
 * <p>This client processes cookies but does not retain them by default.
 * To retain cookies, simply add a cookie store to the HttpContext:</p>
 *
 * <pre>context.setAttribute(ClientContext.COOKIE_STORE, cookieStore);</pre>
 */

using System;
using System.Reflection;
using Android.Content;
using Android.Net;
using Android.OS;
using Java.Lang;
using Exception = System.Exception;

public  class AndroidHttpClient : HttpClient {

	static Type sSslSessionCacheClass;
	static AndroidHttpClient() {
		// if we are on Froyo+ devices, we can take advantage of the SSLSessionCache
		try {
			sSslSessionCacheClass = Class.forName("android.net.SSLSessionCache");
		} catch (Exception e) {
			
		}
	}
	
    // Gzip of data shorter than this probably won't be worthwhile
    public static long DEFAULT_SYNC_MIN_GZIP_BYTES = 256;

    // Default connection and socket timeout of 60 seconds.  Tweak to taste.
    private static  int SOCKET_OPERATION_TIMEOUT = 60 * 1000;

    private static  string TAG = "AndroidHttpClient";


    /** Interceptor throws an exception if the executing thread is blocked */

    private static HttpRequestInterceptor sThreadCheckInterceptor = new HttpRequestInterceptor(delegate(HttpRequest request, HttpContext context)
                                                                                                   {
                                                                                                       // Prevent the HttpRequest from being sent on the main thread
                                                                                                       if (Looper.MyLooper() != null && Looper.MyLooper() == Looper.MainLooper)
                                                                                                       {
                                                                                                           throw new RuntimeException("This thread forbids HTTP requests");
                                                                                                       }
                                                                                                   });

    /**
     * Create a new HttpClient with reasonable defaults (which you can update).
     *
     * @param userAgent to report in your HTTP requests
     * @param context to use for caching SSL sessions (may be null for no caching)
     * @return AndroidHttpClient for you to use for all your requests.
     */
    public static AndroidHttpClient newInstance(string userAgent, Context context) {
        HttpParams someParams = new BasicHttpParams();

        // Turn off stale checking.  Our connections break all the time anyway,
        // and it's not worth it to pay the penalty of checking every time.
        HttpConnectionParams.setStaleCheckingEnabled(someParams, false);

        HttpConnectionParams.setConnectionTimeout(someParams, SOCKET_OPERATION_TIMEOUT);
        HttpConnectionParams.setSoTimeout(someParams, SOCKET_OPERATION_TIMEOUT);
        HttpConnectionParams.setSocketBufferSize(someParams, 8192);

        // Don't handle redirects -- return them to the caller.  Our code
        // often wants to re-POST after a redirect, which we must do ourselves.
        HttpClientParams.setRedirecting(someParams, false);

        object sessionCache = null;
        // Use a session cache for SSL sockets -- Froyo only
        if ( null != context && null != sSslSessionCacheClass ) {
             ConstructorInfo ct;
			try {
				ct = sSslSessionCacheClass.GetConstructor(new[]{typeof(Context)});
				sessionCache = ct.newInstance(context);             
			} catch (SecurityException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (NoSuchMethodException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (IllegalArgumentException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (InstantiationException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (IllegalAccessException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (InvocationTargetException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			}
        }

        // Set the specified user agent and register standard protocols.
        HttpProtocolParams.setUserAgent(someParams, userAgent);
        SchemeRegistry schemeRegistry = new SchemeRegistry();
        schemeRegistry.register(new Scheme("http",
                PlainSocketFactory.getSocketFactory(), 80));
        SocketFactory sslCertificateSocketFactory = null;
        if ( null != sessionCache ) {
        	Method getHttpSocketFactoryMethod;
			try {
				getHttpSocketFactoryMethod = typeof(SSLCertificateSocketFactory).getDeclaredMethod("getHttpSocketFactory",int.TYPE, sSslSessionCacheClass);
	        	sslCertificateSocketFactory = (SocketFactory)getHttpSocketFactoryMethod.invoke(null, SOCKET_OPERATION_TIMEOUT, sessionCache);
			} catch (SecurityException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (NoSuchMethodException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (IllegalArgumentException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (IllegalAccessException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			} catch (InvocationTargetException e) {
				// TODO Auto-generated catch block
				e.PrintStackTrace();
			}
        }
        if ( null == sslCertificateSocketFactory ) {
        	sslCertificateSocketFactory = SSLSocketFactory.getSocketFactory();
        }
        schemeRegistry.register(new Scheme("https",
                sslCertificateSocketFactory, 443));

        ClientConnectionManager manager =
                new ThreadSafeClientConnManager(someParams, schemeRegistry);

        // We use a factory method to modify superclass initialization
        // parameters without the funny call-a-static-method dance.
        return new AndroidHttpClient(manager, someParams);
    }

    /**
     * Create a new HttpClient with reasonable defaults (which you can update).
     * @param userAgent to report in your HTTP requests.
     * @return AndroidHttpClient for you to use for all your requests.
     */
    public static AndroidHttpClient newInstance(string userAgent) {
        return newInstance(userAgent, null /* session cache */);
    }

    private  HttpClient ClientDelegate;

    private RuntimeException mLeakedException = new IllegalStateException(
            "AndroidHttpClient created and never closed");

    private AndroidHttpClient(ClientConnectionManager ccm, HttpParams someParams) {
        this.ClientDelegate = new DefaultHttpClient(ccm, someParams)
        //{
        //    override
        //    protected BasicHttpProcessor createHttpProcessor() {
        //        // Add interceptor to prevent making requests from main thread.
        //        BasicHttpProcessor processor = super.createHttpProcessor();
        //        processor.addRequestInterceptor(sThreadCheckInterceptor);
        //        processor.addRequestInterceptor(new CurlLogger());

        //        return processor;
        //    }

        //    override
        //    protected HttpContext createHttpContext() {
        //        // Same as DefaultHttpClient.createHttpContext() minus the
        //        // cookie store.
        //        HttpContext context = new BasicHttpContext();
        //        context.setAttribute(ClientContext.AUTHSCHEME_REGISTRY,getAuthSchemes());
        //        context.setAttribute(ClientContext.COOKIESPEC_REGISTRY,getCookieSpecs());
        //        context.setAttribute(ClientContext.CREDS_PROVIDER,getCredentialsProvider());
        //        return context;
        //    }
        //}
        ;
    }

    override protected void Finalize() {
        base.finalize();
        if (mLeakedException != null) {
            Log.e(TAG, "Leak found", mLeakedException);
            mLeakedException = null;
        }
    }

    /**
     * Modifies a request to indicate to the server that we would like a
     * gzipped response.  (Uses the "Accept-Encoding" HTTP header.)
     * @param request the request to modify
     * @see #getUngzippedContent
     */
    public static void modifyRequestToAcceptGzipResponse(HttpRequest request) {
        request.addHeader("Accept-Encoding", "gzip");
    }

    /**
     * Gets the input stream from a response entity.  If the entity is gzipped
     * then this will get a stream over the uncompressed data.
     *
     * @param entity the entity whose content should be read
     * @return the input stream to read from
     * @
     */
    public static InputStream getUngzippedContent(HttpEntity entity)
             {
        InputStream responseStream = entity.getContent();
        if (responseStream == null) return responseStream;
        Header header = entity.getContentEncoding();
        if (header == null) return responseStream;
        string contentEncoding = header.getValue();
        if (contentEncoding == null) return responseStream;
        if (contentEncoding.contains("gzip")) responseStream
                = new GZIPInputStream(responseStream);
        return responseStream;
    }

    /**
     * Release resources associated with this client.  You must call this,
     * or significant resources (sockets and memory) may be leaked.
     */
    public void close() {
        if (mLeakedException != null) {
            getConnectionManager().shutdown();
            mLeakedException = null;
        }
    }

    public HttpParams getParams() {
        return ClientDelegate.getParams();
    }

    public ClientConnectionManager getConnectionManager() {
        return ClientDelegate.getConnectionManager();
    }

    public HttpResponse execute(HttpUriRequest request)  {
        return ClientDelegate.execute(request);
    }

    public HttpResponse execute(HttpUriRequest request, HttpContext context)
             {
        return ClientDelegate.execute(request, context);
    }

    public HttpResponse execute(HttpHost target, HttpRequest request)
             {
        return ClientDelegate.execute(target, request);
    }

    public HttpResponse execute(HttpHost target, HttpRequest request,HttpContext context)  {
        return ClientDelegate.execute(target, request, context);
    }

    public T execute<T,U>(HttpUriRequest request,ResponseHandler<U> responseHandler) where U : T{
        return ClientDelegate.execute(request, responseHandler);
    }

    public T execute<T,U>(HttpUriRequest request,ResponseHandler<U> responseHandler, HttpContext context) where U : T
            {
        return ClientDelegate.execute(request, responseHandler, context);
    }

    public  T execute<T,U>(HttpHost target, HttpRequest request,ResponseHandler<U> responseHandler)  where U : T
{
        return ClientDelegate.execute(target, request, responseHandler);
    }

    public  T execute<T,U>(HttpHost target, HttpRequest request,ResponseHandler<U> responseHandler, HttpContext context) where U : T
{
        return ClientDelegate.execute(target, request, responseHandler, context);
    }

    /**
     * Compress data to send to server.
     * Creates a Http Entity holding the gzipped data.
     * The data will not be compressed if it is too short.
     * @param data The bytes to compress
     * @return Entity holding the data
     */
    public static AbstractHttpEntity getCompressedEntity(byte data[], ContentResolver resolver)
             {
        AbstractHttpEntity entity;
        if (data.length < getMinGzipSize(resolver)) {
            entity = new ByteArrayEntity(data);
        } else {
            ByteArrayOutputStream arr = new ByteArrayOutputStream();
            OutputStream zipper = new GZIPOutputStream(arr);
            zipper.write(data);
            zipper.close();
            entity = new ByteArrayEntity(arr.toByteArray());
            entity.setContentEncoding("gzip");
        }
        return entity;
    }

    /**
     * Retrieves the minimum size for compressing data.
     * Shorter data will not be compressed.
     */
    public static long getMinGzipSize(ContentResolver resolver) {
        return DEFAULT_SYNC_MIN_GZIP_BYTES;  // For now, this is just a constant.
    }

    /* cURL logging support. */

    /**
     * Logging tag and level.
     */
    private class LoggingConfiguration {

        private  string tag;
        private  int level;

        private LoggingConfiguration(string tag, int level) {
            this.tag = tag;
            this.level = level;
        }

        /**
         * Returns true if logging is turned on for this configuration.
         */
        private bool isLoggable() {
            return Log.isLoggable(tag, level);
        }

        /**
         * Prints a message using this configuration.
         */
        private void println(string message) {
            Log.println(level, tag, message);
        }
    }

    /** cURL logging configuration. */
    private volatile LoggingConfiguration curlConfiguration;

    /**
     * Enables cURL request logging for this client.
     *
     * @param name to log messages with
     * @param level at which to log messages (see {@link android.util.Log})
     */
    public void enableCurlLogging(string name, int level) {
        if (name == null) {
            throw new NullPointerException("name");
        }
        if (level < Log.VERBOSE || level > Log.ASSERT) {
            throw new IllegalArgumentException("Level is out of range ["+ Log.VERBOSE + ".." + Log.ASSERT + "]");
        }

        curlConfiguration = new LoggingConfiguration(name, level);
    }

    /**
     * Disables cURL logging for this client.
     */
    public void disableCurlLogging() {
        curlConfiguration = null;
    }

    /**
     * Logs cURL commands equivalent to requests.
     */
    private class CurlLogger : HttpRequestInterceptor {
        public void process(HttpRequest request, HttpContext context)
                 {
            LoggingConfiguration configuration = curlConfiguration;
            if (configuration != null
                    && configuration.isLoggable()
                    && request instanceof HttpUriRequest) {
                // Never print auth token -- we used to check ro.secure=0 to
                // enable that, but can't do that in unbundled code.
                configuration.println(toCurl((HttpUriRequest) request, false));
            }
        }
    }

    /**
     * Generates a cURL command equivalent to the given request.
     */
    private static string toCurl(HttpUriRequest request, bool logAuthToken)  {
        StringBuilder builder = new StringBuilder();

        builder.append("curl ");

        foreach (Header header in request.getAllHeaders()) {
            if (!logAuthToken
                    && (header.getName().equals("Authorization") ||
                        header.getName().equals("Cookie"))) {
                continue;
            }
            builder.append("--header \"");
            builder.append(header.toString().trim());
            builder.append("\" ");
        }

        URI uri = request.getURI();

        // If this is a wrapped request, use the URI from the original
        // request instead. getURI() on the wrapper seems to return a
        // relative URI. We want an absolute URI.
        if (request instanceof RequestWrapper) {
            HttpRequest original = ((RequestWrapper) request).getOriginal();
            if (original instanceof HttpUriRequest) {
                uri = ((HttpUriRequest) original).getURI();
            }
        }

        builder.append("\"");
        builder.append(uri);
        builder.append("\"");

        if (request instanceof HttpEntityEnclosingRequest) {
            HttpEntityEnclosingRequest entityRequest =
                    (HttpEntityEnclosingRequest) request;
            HttpEntity entity = entityRequest.getEntity();
            if (entity != null && entity.isRepeatable()) {
                if (entity.getContentLength() < 1024) {
                    ByteArrayOutputStream stream = new ByteArrayOutputStream();
                    entity.writeTo(stream);
                    string entityString = stream.toString();

                    // TODO: Check the content type, too.
                    builder.append(" --data-ascii \"")
                            .append(entityString)
                            .append("\"");
                } else {
                    builder.append(" [TOO MUCH DATA TO INCLUDE]");
                }
            }
        }

        return builder.toString();
    }

    /**
     * Returns the date of the given HTTP date string. This method can identify
     * and parse the date formats emitted by common HTTP servers, such as
     * <a href="http://www.ietf.org/rfc/rfc0822.txt">RFC 822</a>,
     * <a href="http://www.ietf.org/rfc/rfc0850.txt">RFC 850</a>,
     * <a href="http://www.ietf.org/rfc/rfc1036.txt">RFC 1036</a>,
     * <a href="http://www.ietf.org/rfc/rfc1123.txt">RFC 1123</a> and
     * <a href="http://www.opengroup.org/onlinepubs/007908799/xsh/asctime.html">ANSI
     * C's asctime()</a>.
     *
     * @return the number of milliseconds since Jan. 1, 1970, midnight GMT.
     * @throws IllegalArgumentException if {@code dateString} is not a date or
     *     of an unsupported format.
     */
    public static long parseDate(string dateString) {
        return HttpDateTime.parse(dateString);
    }
}