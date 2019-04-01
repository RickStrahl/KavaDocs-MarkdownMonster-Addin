#if false
using System;
using System.IO;
using System.Reflection;

namespace Westwind.TypeImporter
{
	/// <summary>
	/// TypeParserFactory object used to create a TypeParser instance
	/// in another AppDomain. This is done so the classes reflected over
	/// can be unloaded. 
	/// 
	/// Note should use one instance per 
	/// </summary>
	public class TypeParserFactory : MarshalByRefObject,IDisposable	
	{
		/// <summary>
		/// Reference to the AppDomain that the TypeParser
		/// is loaded into.
		/// </summary>
		AppDomain LocalAppDomain = null;

        public string ErrorMessage = "";



		/// <summary>
		/// TypeParser Factory method that loads the TypeParser
		/// object into a new AppDomain so it can be unloaded.
		/// Creates AppDomain and creates type.
		/// </summary>
		/// <returns></returns>
		public TypeParserLegacy CreateTypeParser() 
		{
			if (!CreateAppDomain(null))
				return null;

			// *** Use a custom Assembly Resolver that looks in the CURRENT directory
			//			this.LocalAppDomain.AssemblyResolve += new ResolveEventHandler( TypeParserFactory.ResolveAssembly );

			/// Create the instance inside of the new AppDomain
			/// Note: remote domain uses local EXE's AppBasePath!!!
			TypeParserLegacy parserLegacy = null;

			try 
			{
               Assembly assembly = Assembly.GetExecutingAssembly();               
               string assemblyPath = Assembly.GetExecutingAssembly().Location;
               object objparser = LocalAppDomain.CreateInstanceFrom(assemblyPath,
                                                     typeof(TypeParserLegacy).FullName).Unwrap();

               parserLegacy = (TypeParserLegacy)objparser; 
               
			}
			catch (Exception ex)
			{
                ErrorMessage = ex.GetBaseException().Message;
				return null;
			}

            return parserLegacy;
		}

		private bool CreateAppDomain(string lcAppDomain) 
		{
			if (lcAppDomain == null)
				lcAppDomain = "wwReflection_" + Guid.NewGuid().ToString().GetHashCode().ToString("x");

			AppDomainSetup setup = new AppDomainSetup();

			// *** Point at current directory
            setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            //setup.PrivateBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");

			LocalAppDomain = AppDomain.CreateDomain(lcAppDomain,null,setup);

            // Need a custom resolver so we can load assembly from non current path
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
			
			return true;
		}

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {

                Assembly assembly = Assembly.Load(args.Name);
                if (assembly != null)
                    return assembly;
            }            
            catch { }

            // *** Try to load by filename - split out the filename of the full assembly name
            // *** and append the base path of the original assembly (ie. look in the same dir)
            // *** NOTE: this doesn't account for special search paths but then that never
            //           worked before either.
            string[] Parts = args.Name.Split(',');
            string File = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + Parts[0].Trim() + ".dll";

            return Assembly.LoadFrom(File);
        }

        private Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Assembly assembly = Assembly.ReflectionOnlyLoad(args.Name);
                if (assembly != null)
                    return assembly;
            }
            catch { }

            // *** Try to load by filename - split out the filename of the full assembly name
            // *** and append the base path of the original assembly (ie. look in the same dir)
            // *** NOTE: this doesn't account for special search paths but then that never
            //           worked before either.
            string[] Parts = args.Name.Split(',');
            string File = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + Parts[0].Trim() + ".dll";

            return Assembly.ReflectionOnlyLoadFrom(File);
        }


        /// <summary>
        /// Releases the type parser and unloads the AppDomain
        /// so the assembly effectively gets unloaded
        /// </summary>
        public void UnloadTypeParser() 
		{
			if (LocalAppDomain != null) 
			{
				AppDomain.Unload( LocalAppDomain );
				LocalAppDomain = null;
			}
		}

        public string GetVersionInfo()
        {
            return ".NET Version: " + Environment.Version.ToString() + "\r\n" +
            "wwReflection Assembly: " + typeof(TypeParserFactory).Assembly.CodeBase.Replace("file:///", "").Replace("/", "\\") + "\r\n" +
            "Assembly Cur Dir: " + Directory.GetCurrentDirectory() + "\r\n" +
            "ApplicationBase: " + AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\r\n" +
            "PrivateBinPath: " + AppDomain.CurrentDomain.SetupInformation.PrivateBinPath + "\r\n" +
            "PrivateBinProbe: " + AppDomain.CurrentDomain.SetupInformation.PrivateBinPathProbe + "\r\n" +
            "App Domain: " + AppDomain.CurrentDomain.FriendlyName + "\r\n";
        }

		#region IDisposable Members

		public void Dispose()
		{
			UnloadTypeParser();
		}

		#endregion
	}

}
#endif
