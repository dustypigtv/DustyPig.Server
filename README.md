[![Release](https://github.com/dustypigtv/DustyPig.Server/actions/workflows/release.yml/badge.svg)](https://github.com/dustypigtv/DustyPig.Server/actions/workflows/release.yml)


Swagger: https://service.dustypig.tv/swagger/index.html


<br />
<b>Requirement:</b> you must mount a volume at /config, and put the firebase admin json file at:

```
/config/firebase.json
```

<br />
<br />
<b>Optional:</b> You can mount a volume at /logs to get some error logging. Your can set this environment variable:

```
ASPNETCORE_ENVIRONMENT
```
To 'Development' to see extra info in logs



<br />
<br />

<b>Required Environment Variables</b>

```
MYSQL-SERVER-V3
```
MySql database connection string. Example: Server="mysql";UserID="dp";Password="abc-123";Database="dustypig"

<br />

```
FIREBASE-AUTH-KEY
```
Firebase Auth Client Api key. Example: AbcDe_f123GHi


<br />

```
TMDB-API-KEY
```
TMDB Api Key. Example: AbcDef123GHi


<br />

```
JWT-KEY
```
Encryption key for JWT tokens. Example: F3p4Agakdhsf3234adf

<br />

```
S3-URL
S3-KEY
S3-SECRET
```

S3 Credentials for storing playlist images.

<br />
<br />


<b>Note:</b> Instead of environment variables, you can store all or any of the required variables in a simple json file located in /config/secrets.json.

Example:
```
{
  "FIREBASE-AUTH-KEY": "AbcDe_f123GHi",
  "TMDB-API-KEY": "AbcDef123GHi",
  "S3-URL": "s3.us-central-1.wasabisys.com",
  "S3-SECRET": "my-s3-secret",
  "S3-KEY": "ABC123",
  "MYSQL-SERVER-V3": "Server=\"mysql\";UserID=\"dp\";Password=\"abc-123\";Database=\"dustypig\"",
  "JWT-KEY": "F3p4Agakdhsf3234adf"
}
```