namespace LicenseVerificationLibrary
{
    using System;

    /// <summary>
    /// ResponseData from licensing server.
    /// </summary>
    public class ResponseData
    {
        // Response-specific data.
        #region Public Properties

        /// <summary>
        /// Gets Extra.
        /// </summary>
        public string Extra { get; private set; }

        /// <summary>
        /// Gets NumberUsedOnce.
        /// </summary>
        public int NumberUsedOnce { get; private set; }

        /// <summary>
        /// Gets PackageName.
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// Gets ResponseCode.
        /// </summary>
        public ServerResponseCode ResponseCode { get; private set; }

        /// <summary>
        /// Gets TimeStamp.
        /// </summary>
        public long TimeStamp { get; private set; }

        /// <summary>
        /// Gets UserId.
        /// </summary>
        public string UserId { get; private set; }

        /// <summary>
        /// Gets VersionCode.
        /// </summary>
        public string VersionCode { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Parses response string into ResponseData.
        /// </summary>
        /// <param name="responseData">
        /// response data string
        /// </param>
        /// <returns>
        /// ResponseData object
        /// </returns>
        /// <exception cref="ArgumentException">
        /// upon parsing error
        /// </exception>
        public static ResponseData Parse(string responseData)
        {
            // Must parse out main response data and response-specific data.
            int index = responseData.IndexOf(':');
            string mainData = responseData;
            string extraData = string.Empty;
            if (index != -1)
            {
                mainData = responseData.Substring(0, index);
                extraData = index < responseData.Length ? responseData.Substring(index + 1) : string.Empty;
            }

            string[] fields = mainData.Split('|');
            if (fields.Length < 6)
            {
                throw new ArgumentException("Wrong number of fields.");
            }

            var data = new ResponseData
                {
                    Extra = extraData, 
                    ResponseCode = (ServerResponseCode)Enum.Parse(typeof(ServerResponseCode), fields[0]), 
                    NumberUsedOnce = int.Parse(fields[1]), 
                    PackageName = fields[2], 
                    VersionCode = fields[3], 
                    // Application-specific user identifier.
                    UserId = fields[4], 
                    TimeStamp = long.Parse(fields[5])
                };

            return data;
        }

        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>
        /// The to string.
        /// </returns>
        public override string ToString()
        {
            return string.Join(
                "|", 
                new object[]
                    {
                        (int)this.ResponseCode, this.NumberUsedOnce, this.PackageName, this.VersionCode, this.UserId, 
                        this.TimeStamp
                    });
        }

        #endregion
    }
}