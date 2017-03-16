Usage:

```
<BuildChainer
  BuildDefinitionName = "Name Of Build Definition To Trigger"
  GitBranch = "The name of the branch which should be built"
  GitCommitHash = "The commit hash which should be built"
  VstsAccessToken = "$(SYSTEM_ACCESSTOKEN)"
  VstsTeamProject = "$(SYSTEM_TEAMPROJECT)"
  VstsUrl = "$(SYSTEM_TEAMFOUNDATIONCOLLECTIONURI)"
/>
```

Example:

If the build definition for `MyProject-Mac` needs to invoke
`MyProject-Windows` you could add an msbuild target similar to this and
invoke the target as part of the `MyProject-Mac` build.

```
<Target Name="ChainBuild">
	<BuildChainer
	  BuildDefinitionName = "MyProject-Windows"
	  GitBranch = "$(CALCULATED_BRANCH_NAME)"
	  GitCommitHash = "$(CALCULATED_GIT_HASH)"
	  VstsAccessToken = "$(SYSTEM_ACCESSTOKEN)"
	  VstsTeamProject = "$(SYSTEM_TEAMPROJECT)"
	  VstsUrl = "$(SYSTEM_TEAMFOUNDATIONCOLLECTIONURI)"
	/>
</Target>
```

You will just need to add logic to calculate the current branch and hash.
This information is available through VSTS env vars for builds which are
triggered based on commits in github/vsts, but it is not available if you
manually trigger a commit from the web UI. As such I don't recommend
relying on VSTS supplied environment variables to compute the current branch
or commit.
