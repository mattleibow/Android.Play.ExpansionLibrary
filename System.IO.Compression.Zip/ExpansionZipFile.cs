namespace System.IO.Compression.Zip
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The expansion zip file.
    /// </summary>
    public class ExpansionZipFile
    {
        #region Constants and Fields

        /// <summary>
        /// The files.
        /// </summary>
        private readonly Dictionary<string, ZipFileEntry> files = new Dictionary<string, ZipFileEntry>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpansionZipFile"/> class.
        /// </summary>
        /// <param name="entries">
        /// The entries.
        /// </param>
        public ExpansionZipFile(IEnumerable<ZipFileEntry> entries)
        {
            this.files = entries.ToDictionary(x => x.FilenameInZip.ToUpper(), x => x);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpansionZipFile"/> class.
        /// </summary>
        public ExpansionZipFile()
        {
            this.files = new Dictionary<string, ZipFileEntry>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpansionZipFile"/> class. 
        /// Initializes a new instance of the <see cref="ExpansionZipFile"/> 
        /// class from a collection of zip file paths.
        /// </summary>
        /// <param name="zipPaths">
        /// Collection of zip file paths
        /// </param>
        public ExpansionZipFile(IEnumerable<string> zipPaths)
            : this()
        {
            foreach (string expansionFile in zipPaths)
            {
                this.MergeZipFile(expansionFile);
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Add all the entries from an existing <see cref="ExpansionZipFile"/>
        /// </summary>
        /// <param name="merge">
        /// The ExpansionZipFile to use.
        /// </param>
        public void AddZipFileEntries(IEnumerable<ZipFileEntry> merge)
        {
            foreach (var entry in merge.ToDictionary(x => x.FilenameInZip.ToUpper(), x => x))
            {
                if (this.files.ContainsKey(entry.Key))
                {
                    this.files[entry.Key] = entry.Value;
                }
                else
                {
                    this.files.Add(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// The get all entries.
        /// </summary>
        /// <returns>
        /// A list of all the entries for this app.
        /// </returns>
        public ZipFileEntry[] GetAllEntries()
        {
            return this.files.Values.ToArray();
        }

        /// <summary>
        /// The get entry.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The entry at a specified relative path
        /// </returns>
        public ZipFileEntry GetEntry(string path)
        {
            return this.files[path.ToUpper()];
        }

        /// <summary>
        /// Add all the entries from an existing <see cref="ExpansionZipFile"/>
        /// </summary>
        /// <param name="merge">
        /// The ExpansionZipFile to use.
        /// </param>
        public void MergeZipFile(ExpansionZipFile merge)
        {
            foreach (var entry in merge.files)
            {
                if (this.files.ContainsKey(entry.Key))
                {
                    this.files[entry.Key] = entry.Value;
                }
                else
                {
                    this.files.Add(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Add all the entries from a zip file on the system.
        /// </summary>
        /// <param name="path">
        /// Path to the zip file
        /// </param>
        public void MergeZipFile(string path)
        {
            ZipFileEntry[] merge;
            using (var zip = new ZipFile(path))
            {
                merge = zip.GetAllEntries();
            }

            this.AddZipFileEntries(merge);
        }

        #endregion
    }
}