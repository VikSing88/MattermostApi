using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace MattermostApi {
	public interface ISettings {
		/// <summary>
		/// The Uri of the server (e.g. https://localhost:8065/)
		/// </summary>
		Uri ServerUri { get; }		

		/// <summary>
		/// Authorisation returns this token, which is then used to access the api without having to login
		/// every time.
		/// </summary>
		string AccessToken { get; set; }

		/// <summary>
		/// Set to greater than zero to log all requests going to Basecamp. 
		/// Larger numbers give more verbose logging.
		/// </summary>
		int LogRequest { get; }

		/// <summary>
		/// Set greater than zero to log all replies coming from Basecamp. 
		/// Larger numbers give more verbose logging.
		/// </summary>
		int LogResult { get; }
	}

	public class Settings : ISettings {
		/// <summary>
		/// The Uri of the server (e.g. https://localhost:8065/)
		/// </summary>
		public Uri ServerUri { get; set; }
		
		/// <summary>
		/// Authorisation returns this token, which is then used to access the api without having to login
		/// every time.
		/// </summary>
		public string AccessToken { get; set; }
		
		/// <summary>
		/// Set to greater than zero to log all requests going to Basecamp. 
		/// Larger numbers give more verbose logging.
		/// </summary>
		public int LogRequest { get; set; }

		/// <summary>
		/// Set greater than zero to log all replies coming from Basecamp. 
		/// Larger numbers give more verbose logging.
		/// </summary>
		public int LogResult { get; set; }

		///// <summary>
		///// Any unexpected json items returned will be in here
		///// </summary>
		//[JsonExtensionData]
		//public IDictionary<string, JToken> AdditionalData;

		//[JsonIgnore]
		//public string Filename;
		

		/// <summary>
		/// Check the Settings for missing data.
		/// If you derive from this class you can override this method to add additional checks.
		/// </summary>
		/// <returns>List of error strings - empty if no missing data</returns>
		public virtual List<string> Validate() {
			List<string> errors = new List<string>();
			if (ServerUri == null) {
				errors.Add("ServerUri missing");
			}
			
			return errors;
		}
	}
}
