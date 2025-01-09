using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocHound.Model;
using DocHound.Properties;
using DocHound.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DocHound
{
    /// <summary>
    /// Doc Project manager used to load and save project
    /// </summary>
	public class DocProjectManager 
	{
		public static DocProjectManager Current;

		public DocProject DocTopics { get; set; }

		public string ErrorMessage { get; set; }

		static DocProjectManager()
		{
			Current = new DocProjectManager();
		}

		public static JsonSerializer Serializer
		{
			get
			{                
				if (_serializer == null)
				{
					var settings = new JsonSerializerSettings
					{
						Formatting = Formatting.Indented,
						MissingMemberHandling = MissingMemberHandling.Ignore,
						NullValueHandling = NullValueHandling.Ignore,
                        DefaultValueHandling = DefaultValueHandling.Ignore,  // don't write out default values
						ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
						ContractResolver = new CamelCaseAndIgnoreEmptyEnumerablesResolver()                        
					};
					settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

					_serializer = JsonSerializer.Create(settings);
				}

				return _serializer;
			}
			set { _serializer = value; }
		}
		private static JsonSerializer _serializer;

        

        /// <summary>
        /// Loads a project from 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public DocProject LoadProject(string filename)
        {
            try
			{
				using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					using (var reader = new StreamReader(stream))
					using (var jsonTextReader = new JsonTextReader(reader))
					{
						var project = Serializer.Deserialize<DocProject>(jsonTextReader);
						project.Filename = filename;
					    return project;
					}
				}

			}
			catch (Exception ex)
			{
				SetError($"{DocumentationMonsterResources.FailedToLoadHelpFile}: {ex.GetBaseException().Message}");
			}

			return null;
		}

		public bool SaveProject(DocProject docProject, string filename)
		{
			try
			{
				using(new ProjectWriteLock()) 
				{
				    // retry multiple times on any write failure
				    for (int i = 0; i < 4; i++)
				    {				        
				        using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
				        {
				            using (var writer = new StreamWriter(stream))
				            using (var jsonTextWriter = new JsonTextWriter(writer))
				            {				               
				                try
				                {                                    
				                    Serializer.Serialize(jsonTextWriter, docProject);				                    
				                    break;
				                }
				                catch
				                {
				                    // retry
				                    Task.Delay(15);
				                }
				            }
				        }
				    }
				}
			}
			catch (Exception ex)
			{
                SetError($"{DocumentationMonsterResources.FailedToSaveHelpFile}: {ex.Message}");
			}
			return true;
		}

        /// <summary>
        /// Run async on another thread
        /// </summary>
        /// <param name="docProject"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
	    public async Task<bool> SaveProjectAsync(DocProject docProject, string filename)
	    {
	        return await Task.Run(() => SaveProject(docProject, filename));
	    }

        /// <summary>
        /// Creates a new project. Simply a wrapper around the cref="DocProjectCreator"
        /// </summary>
        /// <param name="creator"></param>
        /// <returns></returns>
	    public static DocProject CreateProject(DocProjectCreator creator)
	    {
	        return creator.CreateProject();
	    }
			
		protected void SetError()
		{
			SetError("CLEAR");
		}

		protected void SetError(string message)
		{
			if (message == null || message == "CLEAR")
			{
				this.ErrorMessage = string.Empty;
				return;
			}
			this.ErrorMessage += message;
		}

		protected void SetError(Exception ex, bool checkInner = false)
		{
			if (ex == null)
				this.ErrorMessage = string.Empty;

			Exception e = ex;
			if (checkInner)
				e = e.GetBaseException();

			ErrorMessage = e.Message;
		}	 
    }




    /// <summary>
    /// JSON Project File Write lock to prevent multiple writes to the 
    /// same file.
    /// </summary>
    public class ProjectWriteLock : IDisposable
    {
        private static object _lock = new object();

        public ProjectWriteLock()
        {
            Monitor.Enter(_lock);
        }

        public void Dispose()
        {
            Monitor.Exit(_lock);
        }
    }

    /// <summary>
    /// Topic file write lock
    /// </summary>
    public class TopicFileUpdateLock : IDisposable
    {
        private static object _lock = new object();

        public TopicFileUpdateLock()
        {
            Monitor.Enter(_lock);
        }

        public void Dispose()
        {
            Monitor.Exit(_lock);
        }
    }


}
