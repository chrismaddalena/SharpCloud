# SharpCloud
SharpCloud is a simple C# utility for checking for the existence of credential files related to Amazon Web Services, Microsoft Azure, and Google Compute.

More information: https://posts.specterops.io/head-in-the-clouds-bd038bb69e48

## Basic Usage

SharpCloud can be run using one of the following commands:

* `SharpCloud.exe all`
    * Searches all user profiles for credentials related to Microsoft Azure, Google Compute, and Amazon Web Services.
* `SharpCloud.exe aws`
    * Searches all user profiles for credentials related to Amazon Web Services.
* `SharpCloud.exe azure`
    * Searches all user profiles for credentials related to Microsoft Azure.
* `SharpCloud.exe gcloud`
    * Searches all user profiles for credentials related to Google Compute.

## SharpCloud with Aggressor

If you use Cobalt Strike, this repo includes a sharpcloud.cna file for CS. This adds sveral aliases for `execute_assembly` with SharpCloud.exe:

* `dump_aws`
* `dump_gcloud`
* `dump_azure`

The SharpCloud.exe binary needs to be in the same directory as the script.

The aliases are fairly self-explanatory. As an example, `dump_aws` is an alias for `execute_assembly SharpCloud.exe aws`. While it would be trivial to set aside the C# and write SharpCloud using shell or PowerShell commands, this was not done to keep SharpCloud's checks and data collection as stealthy as possible. That means avoiding command line logging.

It is notable that `dump_aws` will add any discovered credentials to Cobalt Strike's Credentials model. Should the alias find AWS credentials, those credentials will be saved just like credentials discovered via Mimikatz and other Cobalt Strike utilities. They will appear with the `realm` set to "AWS" and the access key and access secret set as the `user` and `password`. If an AWS token is present in the profile, the token will be noted in the `password` field. The AWS profile name will be saved in the `source` field.

This is only done for AWS credentials, but might be done for Azure in a future version. It's not feasible for Google Compute because Compute uses SQLite3 databases and reading the values from them becomes much trickier. It is possible, and potentially useful, to do this for credential information found inside Compute's legacy_credential directory.