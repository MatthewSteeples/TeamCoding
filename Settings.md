# Settings

There are shared settings (AKA Sync Settings), used to share IDE actions and User settings, used to control how you appear to other users. User Settings are optional, Shared Settings are not; See below for details.

## Shared Settings

In order to see other user's actions within the IDE you need to set up at least one method of syncronisation. You can use multiple methods in case one is intermittently unavailable although this is a rare use-case.

- **UNC File Share**.  
Define a path to a shared folder. Files will be placed in this folder to send changes and new/changed files will be picked up to receive changes. Read/write access required.
- **Redis**.  
Define the redis configuration string (refer to Redis docs for details. In the simple case this can just be the hostname of the redis server). The publish/subscribe method will be used to send and receive changes.
- **Slack**.  
Define the slack bot token and channel. Messages will be posted to the channel specified by the slack bot idenfitied by the access token. There will be a lot of messages, so it's recommended for you to unsubscribe from the channel. **To create the channel and bot on slack**:
  - First create the bot, so when we add the channel we can add it in one step.
  - To create the bot click your team name and choose `Apps and Integrations`, then click `Build`. You should end up at `https://yourteamname.slack.com/apps/build`.
  - Next under `Something just for my team` click `Make a Custom Integration` and on the following page choose `Bots`.
  - Decide upon a bot name; this can be anything you like (within Slack's restrictions). I've been using `teamcoding`. Click `Add bot integration`.
  - You will then see an `API Token` of the form `xoxb-00000000000-a0a0a0a0a0a0a0a0a0a0a0a0` (starting with `xoxb-` followed by some numbers followed by some letters and numbers. This is what you should put as the Slack Token in the Team Coding configuration. Remember to follow Slack's recommendations of not putting this token in a public repository (as part of the `TeamCoding.json` config file for example; use user/Visual Studio settings`.
  - If you want to you can customise the bot further, giving an image and description etc, but this is not required.
  - Click `Save Integration`. Once saved you can now go back to your team's message screen.
  - Create a Slack channel. Click the + next to Channels when in your team's mesage screen. Name the channel and invite the bot created earlier. The channel name (including leading hash) will be what you enter in the Team Coding configuration. You will want to turn off notifications for the channel.
- **SQL Server**
Define an SQL Server connection string. This should connect to a database with a [dbo].[TeamCodingSync] table as defined in [SqlServerPersisterCreateScript.sql](https://github.com/georgeduckett/TeamCoding/blob/master/TeamCoding/Resources/SqlServerPersisterCreateScript.sql). Note, the service broker must be enabled for the database.
- **Windows Service / Console Application**
Specify the IP address and Port that the service/console app is running on, e.g. 192.168.0.123:23023 The listening port is currently fixed at 23023.
You can download the service/console app at https://github.com/georgeduckett/TeamCoding/releases, look for the TeamCoding.WindowsService.zip file and extract that on the machine you want to use.
To run it as a console application you can just run the exe, however if you run it from Windows Task Scheduler you must pass \c as an argument, the RunAsConsole.bat file provided will do that.
To install or uninstall it as a windows service use the Install.ps1 and Uninstall.ps1 scripts. These will create or clean up a TeamCoding Sync folder in Program Files, and register/deregister the exe as a service.
To manually register or deregister the service launch the program as an administrator passing \i or \u.

## User Settings

Using the user settings you can customise how you appear to others.

- **Username**.  
The username people see when hovering over your avatar that appears on a tab. The first letter is used as the avatar if no image url is specified.
- **Image URL**.  
The URL of an image to use as your avatar that appears on a tab or within a document.
- **User Tab Display / User Code Display**
Controls how other users appear to you in the tabs and within code. Possible values are:
  - Avatar: Show an image representing the user (if possible)
  - Letter: Show the initial letter of the user's name.
  - Colour: Show a consistent colour for each user.
