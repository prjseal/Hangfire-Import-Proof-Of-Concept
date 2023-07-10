<img style="height:30px; width: auto;" src="https://github.com/prjseal/Hangfire-Import-Proof-Of-Concept/assets/9142936/70254e13-b727-40a9-8120-5ba613a5fa8f" alt="ClerksWell Logo" />

# Hangfire Import Proof of Concept

This is a proof of concept project for me to learn and share how to do imports using hangfire jobs.

## What technology is installed?

We have an Umbraco 10.5.1 website, with the packages `uSync` and `Cultiv.Hangfire` installed
uSync is just to help you get setup, but the main thing in this project is the `Cultiv.Hangfire` package which gives you a user interface and visual dashboard to see the queue and status of jobs.

![image](https://github.com/prjseal/Hangfire-Import-Proof-Of-Concept/assets/9142936/82ad5aaf-d94f-4863-9cd1-1107af929e7a)

I have followed the steps outlined in this [skrift article](https://skrift.io/issues/performance-and-availability-improvements-of-umbraco-v10-websites-with-hangfire-jobs/) by Nurhak Kaya

## How to set it up.

1. Fork the repository
2. Clone it to your machine
3. Create a database in SQLEXPRESS on your machine and call it `hangfiredb` create a SQL login that has `dbo` permissions and give it a username of `hangfire` and password of `hangfire` (You can specify your own connection string but it needs to be a SQL Server or SQL Express database. Hangfire doesn't work with SQLCE, SQLite or LocalDb).
4. Build the solution and run it

uSync should have created the content for you and hangfire should have created the required tables for you in the separate hangfire database.

![image](https://github.com/prjseal/Hangfire-Import-Proof-Of-Concept/assets/9142936/27e42928-8555-430c-b3f0-ef46c946c610)

In the content section you will have an Import folder and depending on how long it took for you to check you may also have some items inside it.

### Before Import

![image](https://github.com/prjseal/Hangfire-Import-Proof-Of-Concept/assets/9142936/2a464a30-5f36-4dde-9a6b-8493580cfc2f)

### After Import

![image](https://github.com/prjseal/Hangfire-Import-Proof-Of-Concept/assets/9142936/5be2240c-e304-40c3-9848-3a48e8c38a9d)

## What's happening?

There is a file in the wwwroot folder called `centres.json`. There is an import task that runs every minute and is checking that centres.json file as it if were a response from an external API somewhere.

```js
[
  {
    "name": "Hoxton Docks",
    "latitude": 51.53565,
    "longitude": -0.07124,
    "lastModifiedDate": "2023-07-07",
    "systemid": "2c79deb6-7868-45e7-b657-777e6c2ef711"
  },
  {
    "name": "Techspace Shoreditch",
    "latitude": 51.52414,
    "longitude": -0.08274,
    "lastModifiedDate": "2023-07-07",
    "systemid": "79ead865-30d2-41dd-8737-adc675d99484"
  },
  {
    "name": "Barbican Centre",
    "latitude": 51.52031,
    "longitude": -0.09381,
    "lastModifiedDate": "2023-07-07",
    "systemid": "f9a7d759-b020-4483-bdbf-9411cc693d73"
  },
  {
    "name": "Everyman Kings Cross",
    "latitude": 51.53752,
    "longitude": -0.12401,
    "lastModifiedDate": "2023-07-07",
    "systemid": "f9a7d123-b020-4483-bdbf-9411cc693d73"
  }
]
```

It checks to see if there are any new centres added or any changes to the centres that are there and then if there are it adds them to the hangfire queue to create or update them.
It then does a save and publish with children on the Import Folder.

## How can I test it?

Try adding a centre in the `centres.json` file, paste it below, edit it and then save the file. Then you can go to the hangfire dashboard in the settings section and see if the import task creates any new jobs like create or update.

```json
,
  {
    "name": "Metal Box Factory",
    "latitude": 51.50473,
    "longitude": -0.09706,
    "lastModifiedDate": "2023-07-09",
    "systemid": "b5a7a222-a212-5637-acdf-2231cb723d14"
  }
```

Then when they have finished running you can check the content section again to see your content item added or updated.

![image](https://github.com/prjseal/Hangfire-Import-Proof-Of-Concept/assets/9142936/8380054a-cc6d-437f-a1bd-2c1f618e342f)

## JobsComposer.cs

This file is responsible for scheduling the Import task to run every minute and it contains a cron expression for how often to run it. [What is a Cron expression?](https://en.wikipedia.org/wiki/Cron)

```cs
using Hangfire;
using MyProject.Services;
using Umbraco.Cms.Core.Composing;

namespace MyProject.Composers;

public class JobsComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        if(AllowRunningHangfireJobs(builder))
        {
            RecurringJob.AddOrUpdate<IImportService>("Import", x => x.Import(), "*/1 * * * *");
        }
    }

    private bool AllowRunningHangfireJobs(IUmbracoBuilder builder)
    {
        var config = builder.Services.BuildServiceProvider().GetService<IConfiguration>();
        return config.GetValue<bool>("AllowRunningHangfireJobs");
    }
}
```

## Import Service

In the services folder, there is an interface called IImportService and a concrete class called ImportService which implements the interface.
You can write your own logic, but a key part of the import method to note is where it adds the individual create or update tasks to the queue.

```cs
if (data != null)
{
    var needToPublish = false;

    foreach (var centre in data)
    {
        if(!existingPages.ContainsKey(centre.systemid.ToString()))
        {
            //create an umbraco node
            Hangfire.BackgroundJob.Enqueue<IImportService>(x => x.ImportSingleCentre(centre, rootContent.Id));
            needToPublish = true;
        }
        else
        {
            //update the umbraco node
            if (!existingPages.TryGetValue(centre.systemid.ToString(), out var contentItem)) continue;

            var lastUpdatedDate = contentItem.GetValue<DateTime>("lastModifiedDate");

            if (lastUpdatedDate >= centre.lastModifiedDate) continue;

            Hangfire.BackgroundJob.Enqueue<IImportService>(x => x.UpdateSingleCentre(centre, contentItem.Id));
            needToPublish = true;
        }
    }

    if(needToPublish)
    {
        Hangfire.BackgroundJob.Enqueue<IImportService>(x => x.PublishImportFolderAndChildren());
    }
}
```

## Credits

Thanks to Nurhak Kaya from Great State for helping me get set up with this and to ClerksWell for giving me the time to work on this and publish it. Not forgetting Sebastiaan Janssen who made the `Cultiv.Hangfire` package.
