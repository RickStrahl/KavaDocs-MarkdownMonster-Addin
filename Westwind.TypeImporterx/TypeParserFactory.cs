using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Specialized;

using System.IO;
using System.Text;
using System.Reflection;
using System.Xml;

using System.Runtime.Remoting;

using Westwind.wwReflection.Tools;

namespace Westwind.wwReflection
{
	/// <summary>
	/// TypeParserFactory object used to create a TypeParser instance
	/// in another AppDomain. This is done so the classes reflected over
	/// can be unloaded. 
	/// 
	/// Note should use one instance per 
	/// </summary>
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[ProgId("wwReflection.TypeParserFactory")]
	public class TypeParserFactory : System.MarshalByRefObject,IDisposable	
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
		public TypeParser CreateTypeParser() 
		{
			if (!CreateAppDomain(null))
				return null;

			// *** Use a custom Assembly Resolver that looks in the CURRENT directory
			//			this.LocalAppDomain.AssemblyResolve += new ResolveEventHandler( TypeParserFactory.ResolveAssembly );

			/// Create the instance inside of the new AppDomain
			/// Note: remote domain uses local EXE's AppBasePath!!!
			TypeParser parser = null;

			try 
			{
               Assembly assembly = Assembly.GetExecutingAssembly();               
               string assemblyPath = Assembly.GetExecutingAssembly().Location;
               object objparser = this.LocalAppDomain.CreateInstanceFrom(assemblyPath,
                                                     typeof(TypeParser).FullName).Unwrap();

               parser = (TypeParser)objparser; 
               
			}
			catch (Exception ex)
			{
                this.ErrorMessage = ex.GetBaseException().Message;
				return null;
			}

            return parser;
		}

		private bool CreateAppDomain(string lcAppDomain) 
		{
			if (lcAppDomain == null)
				lcAppDomain = "wwReflection_" + Guid.NewGuid().ToString().GetHashCode().ToString("x");

			AppDomainSetup setup = new AppDomainSetup();

			// *** Point at current directory
            setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            //setup.PrivateBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");

			this.LocalAppDomain = AppDomain.CreateDomain(lcAppDomain,null,setup);

            // Need a custom resolver so we can load assembly from non current path
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += CurrentDomain_ReflectionOnlyAssemblyResolve;
			
			return true;
		}

        private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {

                Assembly assembly = System.Reflection.Assembly.Load(args.Name);
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

            return System.Reflection.Assembly.LoadFrom(File);
        }

        private System.Reflection.Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                Assembly assembly = System.Reflection.Assembly.ReflectionOnlyLoad(args.Name);
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

            return System.Reflection.Assembly.ReflectionOnlyLoadFrom(File);
        }


        /// <summary>
        /// Releases the type parser and unloads the AppDomain
        /// so the assembly effectively gets unloaded
        /// </summary>
        public void UnloadTypeParser() 
		{
			if (this.LocalAppDomain != null) 
			{
				AppDomain.Unload( this.LocalAppDomain );
				this.LocalAppDomain = null;
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
			this.UnloadTypeParser();
		}

		#endregion
	}

}
