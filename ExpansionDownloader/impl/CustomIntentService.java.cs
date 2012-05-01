namespace ExpansionDownloader.impl
{
    using Android.App;
    using Android.Content;
    using Android.OS;

    using Java.Lang;

    using Debug = System.Diagnostics.Debug;

    /// <summary>
    /// The custom intent service.
    /// </summary>
    public abstract class CustomIntentService : Service
    {
        #region Constants and Fields

        /// <summary>
        /// The m name.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// The wha t_ message.
        /// </summary>
        private const int WhatMessage = -10;

        /// <summary>
        /// The m redelivery.
        /// </summary>
        private bool redelivery;

        /// <summary>
        /// The m service handler.
        /// </summary>
        private volatile ServiceHandler serviceHandler;

        /// <summary>
        /// The m service looper.
        /// </summary>
        private volatile Looper serviceLooper;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomIntentService"/> class.
        /// </summary>
        /// <param name="paramString">
        /// The param string.
        /// </param>
        protected CustomIntentService(string paramString)
        {
            this.name = paramString;
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The on bind.
        /// </summary>
        /// <param name="paramIntent">
        /// The param intent.
        /// </param>
        /// <returns>
        /// </returns>
        public override IBinder OnBind(Intent paramIntent)
        {
            return null;
        }

        /// <summary>
        /// The on create.
        /// </summary>
        public override void OnCreate()
        {
            base.OnCreate();
            var localHandlerThread = new HandlerThread("IntentService[" + this.name + "]");
            localHandlerThread.Start();
            this.serviceLooper = localHandlerThread.Looper;
            this.serviceHandler = new ServiceHandler(this.serviceLooper, this);
        }

        /// <summary>
        /// The on destroy.
        /// </summary>
        public override void OnDestroy()
        {
            Thread localThread = this.serviceLooper.Thread;
            if ((localThread != null) && localThread.IsAlive)
            {
                localThread.Interrupt();
            }

            this.serviceLooper.Quit();
            Debug.WriteLine("onDestroy");
        }

        /// <summary>
        /// The on start.
        /// </summary>
        /// <param name="paramIntent">
        /// The param intent.
        /// </param>
        /// <param name="startId">
        /// The start id.
        /// </param>
        public override void OnStart(Intent paramIntent, int startId)
        {
            if (!this.serviceHandler.HasMessages(WhatMessage))
            {
                Message localMessage = this.serviceHandler.ObtainMessage();
                localMessage.Arg1 = startId;
                localMessage.Obj = paramIntent;
                localMessage.What = WhatMessage;
                this.serviceHandler.SendMessage(localMessage);
            }
        }

        /// <summary>
        /// The on start command.
        /// </summary>
        /// <param name="paramIntent">
        /// The param intent.
        /// </param>
        /// <param name="flags">
        /// The flags.
        /// </param>
        /// <param name="startId">
        /// The start id.
        /// </param>
        /// <returns>
        /// </returns>
        public override StartCommandResult OnStartCommand(Intent paramIntent, StartCommandFlags flags, int startId)
        {
            this.OnStart(paramIntent, startId);
            return this.redelivery ? StartCommandResult.RedeliverIntent : StartCommandResult.NotSticky;
        }

        /// <summary>
        /// The set intent redelivery.
        /// </summary>
        /// <param name="enabled">
        /// The enabled.
        /// </param>
        public void SetIntentRedelivery(bool enabled)
        {
            this.redelivery = enabled;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The on handle intent.
        /// </summary>
        /// <param name="paramIntent">
        /// The param intent.
        /// </param>
        protected abstract void OnHandleIntent(Intent paramIntent);

        /// <summary>
        /// Checks to see if the service should the sevice stop.
        /// </summary>
        /// <returns>
        /// True if the service should stop, otherwise false.
        /// </returns>
        protected abstract bool ShouldStop();

        #endregion

        /// <summary>
        /// The service handler.
        /// </summary>
        private class ServiceHandler : Handler
        {
            #region Constants and Fields

            /// <summary>
            /// The customIntentService.
            /// </summary>
            private readonly CustomIntentService customIntentService;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            /// Initializes a new instance of the <see cref="ServiceHandler"/> class.
            /// </summary>
            /// <param name="looper">
            /// The looper.
            /// </param>
            /// <param name="customIntentService">
            /// The customIntentService.
            /// </param>
            public ServiceHandler(Looper looper, CustomIntentService customIntentService)
                : base(looper)
            {
                this.customIntentService = customIntentService;
            }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            /// The handle message.
            /// </summary>
            /// <param name="paramMessage">
            /// The param message.
            /// </param>
            public override void HandleMessage(Message paramMessage)
            {
                this.customIntentService.OnHandleIntent((Intent)paramMessage.Obj);
                if (this.customIntentService.ShouldStop())
                {
                    Debug.WriteLine("stopSelf");
                    this.customIntentService.StopSelf(paramMessage.Arg1);
                    Debug.WriteLine("afterStopSelf");
                }
            }

            #endregion
        }
    }
}