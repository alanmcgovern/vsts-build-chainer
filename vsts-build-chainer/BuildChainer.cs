using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace VstsBuildChainer
{
    public class BuildChainer : Microsoft.Build.Utilities.Task
    {
        const string VstsApiVersion = "2.0";

        [Required]
        public string BuildDefinitionName { // Set by user
            get; set;
        }

        [Required]
        public string GitBranch { // Set by user
            get; set;
        }

        [Required]
        public string GitCommitHash { // Set by user
            get; set;
        }

        [Required]
        public string VstsAccessToken { // VSTS env var: SYSTEM_ACCESSTOKEN
            get; set;
        }

        [Required]
        public string VstsTeamProject { // VSTS env var: SYSTEM_TEAMPROJECT
            get; set;
        }

        [Required]
        public string VstsUrl { // VSTS env var: SYSTEM_TEAMFOUNDATIONCOLLECTIONURI
            get; set;
        }

        public override bool Execute()
        {
            var buildDefinitionId = GetDefinitionId ();
            if (!string.IsNullOrEmpty (buildDefinitionId))
                QueueBuild (buildDefinitionId);
            return !Log.HasLoggedErrors;
        }

        string GetDefinitionId ()
        {
            // GET https://{instance}/DefaultCollection/{project}/_apis/build/definitions?api-version={version}[&name={string}][&type={string}]
            var address = $"{VstsUrl}DefaultCollection/{VstsTeamProject}/_apis/build/definitions?api-version={VstsApiVersion}&name={BuildDefinitionName}";

            using (var client = CreateClient ())
            using (var result = client.GetAsync (address).Result) {
                result.EnsureSuccessStatusCode ();

			    var stringContent = result.Content.ReadAsStringAsync ().Result;
                var document = Newtonsoft.Json.JsonConvert.DeserializeXNode (stringContent, "root");
               
                var count = document.Root.Element ("count");
                if (count == null) {
                    Log.LogError ($"The '{BuildDefinitionName}' definition did not exist.");
                    return null;
                }

                 if (count.Value != "1") {
                    Log.LogError ($"Could not locate exactly one instance of the '{BuildDefinitionName}' definition.");
                    return null;
                }
                var id = document.Root.Element ("value").Element ("id");
                if (id == null) {
                    Log.LogError ($"Could not determine the ID of the {BuildDefinitionName} definition");
                    return null;
                }

                return id.Value;
            }
        }

        void QueueBuild(string buildDefinitionId)
        {
            string address = $"{VstsUrl}/DefaultCollection/{VstsTeamProject}/_apis/build/builds?api-version={VstsApiVersion}";
			string content = $@"
{{
  ""definition"": {{
    ""id"": {buildDefinitionId}
  }},
  ""sourceBranch"": ""{GitCommitHash}"",
  ""parameters"": ""{{\""Build.SourceBranchName\"":\""{GitBranch}\""}}""
}}";

			using (var client = CreateClient ())
			using (var result = client.PostAsync (address, new StringContent (content, Encoding.UTF8, "application/json")).Result)
                result.EnsureSuccessStatusCode ();
        }

        HttpClient CreateClient ()
        {
            var client = new HttpClient ();
			client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue("application/json"));

			var pac = Convert.ToBase64String (ASCIIEncoding.ASCII.GetBytes (string.Format("{0}:{1}", "", VstsAccessToken)));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", pac);
			return client;
        }
    }
}
