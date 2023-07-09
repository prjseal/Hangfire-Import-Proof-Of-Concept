# Hangfire-Import-Proof-Of-Concept

This is a proof of concept project to lear how to do imports using hangfire jobs.

## How to set it up.

1. Fork the repository
2. Clone it to your machine
3. Create a database in SQLEXPRESS on your machine and call it `hangfire` create a SQL login that has `dbo` permissions and give it a username of `hangfire` and password of `hangfire` (You can specify your own connection string but it needs to be a SQL Server or SQL Express database. Hangfire doesn't work with SQLCE, SQLite or LocalDb).
4. Build the solution and run it

uSync should have created the content for you and hangfire should have created the required tables for you in the separate hangfire database.

In the content section you will have an Import folder and depending on how long it took for you to check you may also have some items inside it.

## What's happening?

There is a file in the wwwroot folder called `centres.json`. There is an import task that runs every minute and is checking that centres.json file as it if were a response from an external API somewhere.
It checks to see if there are any new centres added or any changes to the centres that are there and then if there are it adds them to the hangfire queue to create or update them.
It then does a save and publish with children on the Import Folder.

## How can I test it?

Try copying a centre in the centres.json file, paste it below, edit it and then save the file. Then you can go to the hangfire dashboard in the settings section and see if the import task creates any new jobs like create or update.

Then when they have finished running you can check the content section again to see your content item added or updated.
