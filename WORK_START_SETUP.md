# memoQ work-start history action

This source package extends the uploaded Blackbird memoQ connector with:

**Project history → Get project work-start events**

Status: source implementation prepared; compilation, .NET tests, deployment and
live memoQ verification have NOT been performed. This environment has no .NET SDK.
This is a source ZIP, not the compiled ZIP that Blackbird accepts for deployment.

## What it reads

The action calls the connector's existing SOAP client method
`GetDocumentHistoryAsync(projectId, request)` and keeps only entries whose type is
`DocumentHistoryItemType.WorkStartedOnProject`.

It reuses the existing memoQ connection and error/retry handling. It makes no
changes to projects, files, assignments, translation status or history.

Inputs:

| Input | Required | Meaning |
| --- | --- | --- |
| Project ID | Yes | memoQ project GUID, selected from the existing project dropdown or mapped from Search projects |
| Document or slice IDs | No | memoQ document/slice GUID collection; omitted or empty means all project documents |
| User ID | No | Filter returned work-start events to one memoQ user GUID |

Outputs: `Events` and `Count`. Each event contains `ProjectId`, `DocumentId`,
`DocumentName`, `UserId`, `StartedAt`, and `EventType`.

All recorded matching entries are returned, sorted by timestamp ascending.
Repeated entries are retained. A missing/empty document or user GUID becomes null.
User names are not embedded in the history response: map a nonempty `UserId` into
the existing **Get user** action and use its **User name** or **Full name** output.

Timestamps are preserved exactly as supplied by the SOAP client. The code does
not assume they are UTC or apply a timezone conversion. Compare one known event
with memoQ's History screen before interpreting times across time zones.

This records a memoQ work-start event. It does not establish hours worked or
identify whether the participant was translating or reviewing. In the supplied
screenshot, that person had a Reviewer1 assignment; do not label every entry a
translation start without separately checking assignments.

## Build and unit tests (Windows PowerShell)

Install the .NET 8 SDK, extract this source ZIP, and open a terminal in MemoQ-main.
NuGet package restore requires network access.

```powershell
dotnet restore Apps.Memoq/Apps.MemoQ.csproj
dotnet build Apps.Memoq/Apps.MemoQ.csproj --configuration Release
dotnet test Tests.MemoQ/Tests.MemoQ.csproj --configuration Release --filter "FullyQualifiedName~WorkStartEventMapperTests"
```

The new mapper tests use synthetic data and require no credentials. They cover
event filtering, ordering, timestamp kind preservation, output fields, user
filtering, repeated events, missing IDs, and empty history. The original suite
contains live integration tests, including operations that change server data;
the filter above runs only the new isolated tests.

## Package and deploy your custom app

The project has unique custom identifiers, as required by Blackbird for forks:

- Product: `memoQ Work Start OAJ678d6c1c`
- PackageId: `OAJ678d6c1c.MemoQ.WorkStart`
- AssemblyName: `OAJ678d6c1c.MemoQ.WorkStart`
- Version: `1.5.14`

Namespaces remain unchanged. For future custom versions, increment Version.
For an upstream contribution, omit the custom csproj identity changes from the PR.

Publish portable output, without a runtime identifier:

```powershell
dotnet publish Apps.Memoq/Apps.MemoQ.csproj --configuration Release --no-self-contained --output publish-work-start
Compress-Archive -Path publish-work-start/* -DestinationPath memoq-work-start-build.zip -Force
```

Check that the compiled ZIP is below Blackbird's current upload limit (the
documentation states builds up to 15 MB inclusive are accepted). In Blackbird:
**Apps → My custom apps → Create app**, then upload the compiled ZIP. For later
versions, use **New version → Update App**. You need custom-app deployment access
in your organization. The packaged upstream workflow depends on Blackbird-owned
repository variables/secrets; use the manual process above for this custom fork.

Create a connection for the custom app using the same memoQ WS API URL and API
key configuration as your existing connection. The official memoQ app remains
separate. This feature uses an action; inherited webhook features have not been
validated for this fork and are not needed by the reporting workflow.

## First live test

1. Create a manually triggered Bird.
2. Add the custom app's **Get project work-start events** action.
3. Select the project shown in your screenshot. Leave both optional filters empty.
4. Run the Bird and inspect `Events` and `Count`.
5. Compare the relevant row with the highlighted History entry: Magdalena Bellini,
   February 2, 2026 at 2:52:45 PM, Grievance 3997 Resolution Letter - Translation.docx
   (spa-US). Resolve the returned User ID through **Get user** if needed.
6. Check the timezone interpretation and document ID against the server. The
   screenshot's displayed time is a comparison reference, not an assumed UTC time.
7. Test the document and user filters, then a project with no work-start records.

If authentication or SOAP faults occur, retain the error and server version for
troubleshooting; do not interpret a failed request as zero work-start events.

## List across multiple projects

Use a manual or scheduled Bird:

1. **Search projects**.
2. Loop over the returned Projects collection.
3. Map each project's GUID into **Get project work-start events**.
4. Loop over Events, optionally resolving each distinct User ID once with Get user.
5. Write selected fields to your Excel/Sheets report.

When running repeatedly, use an upsert key based on project ID, document/slice ID,
user ID and the full recorded timestamp. The history API shown in this connector
has no event-ID field, so this key cannot distinguish truly identical history
rows. For a faithful historical export, replace the report snapshot each run
instead of appending. The action is stateless and returns existing history on
every call; it is not a new-event trigger.

## Changed source files

- Apps.Memoq/Actions/ProjectHistoryActions.cs — action and GUID input validation
- Apps.Memoq/Models/ProjectHistory/WorkStartEventsRequest.cs — optional filters
- Apps.Memoq/Models/ProjectHistory/WorkStartEventsResponse.cs — report outputs
- Apps.Memoq/Utils/ProjectHistory/WorkStartEventMapper.cs — filtering and mapping
- Tests.MemoQ/WorkStartEventMapperTests.cs — isolated mapper tests
- Apps.Memoq/Apps.MemoQ.csproj — custom fork identity and version
- README.md — new action documentation
- WORK_START_SETUP.md — this guide

No generated SOAP contracts were changed. The existing contract already includes
the method and event type needed for this feature.

## References

- Source connector: https://github.com/bb-io/MemoQ
- Blackbird deployment: https://docs.blackbird.io/sdk/deploying/
- memoQ method: https://docs.memoq.com/current/api-docs/wsapi/api/serverprojectservice/MemoQServices.IServerProjectService.html
- Event types: https://docs.memoq.com/current/api-docs/wsapi/api/serverprojectservice/MemoQServices.DocumentHistoryItemType.html
