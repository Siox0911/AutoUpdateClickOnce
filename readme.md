# ClickOnce Auto Update from Version .Net 8 and higher<hr/>
## Description
This is a simple example that allows you to add automatic update
functionality to your .NET applications with a ClickOnce Deployment.
It is compatible with any application that uses the .NET 8.0 or higher.
It is written in C#.

This is an example of how to use the class: `ApplicationDeployment.cs`
This example is a fully automatic update without any questions.
Remember that the current application is not properly closed if
the update command is executed. It is simply shot down.

## Beschreibung
Dies ist ein einfaches Beispiel, mit dem Sie Ihren .NET Anwendungen mit
einer ClickOnce Bereitstellung, eine automatische Aktualisierungsfunktion
hinzufügen können. Es ist mit jeder Anwendung kompatibel, die .NET 8.0
oder höher verwendet. Es ist in C# geschrieben.

Dies ist ein Beispiel für die Verwendung der Klasse `ApplicationDeployment.cs`: Dieses Beispiel
ist ein vollautomatisches Update ohne Fragen. Denken Sie daran, dass die
aktuelle Anwendung nicht ordnungsgemäß geschlossen wird, wenn der Befehl
Update ausgeführt wird. Sie wird einfach abgeschossen.

```csharp
if(ApplicationDeployment.IsNetworkDeployed)
{
    var appDeployment = ApplicationDeployment.CurrentDeployment;

    //use try catch, may the UpdateLocation is not available
    try
    {
        await appDeployment.CheckForUpdateAsync();
    }
    catch(Exception ex)
    {
        //Log the exception
        Console.WriteLine(ex.Message);
    }

    if(appDeployment.IsUpdateAvailable)
    {
        //Save and export settings
        SaveSettings(); //SaveSettings is not a part of the ApplicationDeployment or ExportHelper class
        var settingsExportHelper = new SettingsExportHelper(Properties.Settings.Default, @"C:\Temp\ExportSettings.json");
        //true means that is human readable (well formated)
        settingsExportHelper.Export(true);
        
        //Update the application
        appDeployment.Update();
    }
}
```

### Thanks to Microsoft for the example
[https://docs.microsoft.com/en-us/visualstudio/deployment/how-to-check-for-application-updates-programmatically-using-the-clickonce-deployment-api?view=vs-2022](https://docs.microsoft.com/en-us/visualstudio/deployment/how-to-check-for-application-updates-programmatically-using-the-clickonce-deployment-api?view=vs-2022)
The class `ApplicationDeployment.cs` is based on the example from Microsoft.
I have added some corrections like `nullable` properties and `async` methods.

### Danke an Microsoft für das Beispiel
[https://docs.microsoft.com/de-de/visualstudio/deployment/how-to-check-for-application-updates-programmatically-using-the-clickonce-deployment-api?view=vs-2022](https://docs.microsoft.com/de-de/visualstudio/deployment/how-to-check-for-application-updates-programmatically-using-the-clickonce-deployment-api?view=vs-2022)
Die Klasse `ApplicationDeployment.cs` basiert auf dem Beispiel von Microsoft.
Ich habe einige Korrekturen wie `nullable` Eigenschaften und `async` Methoden hinzugefügt.

## License
[MIT](https://choosealicense.com/licenses/mit/) License

### CoCreater (en)
Oh, he don't nothing about it. I have used some code of his WpfSettings. Modified but used. This is linked in the documentation.

derskythe   
[![N|Solid](https://avatars.githubusercontent.com/u/913534?s=60&v=4)](https://github.com/derskythe/WpfSettings/)

### CoCreater (de)
Oh, er weiß nichts davon. Ich habe etwas Code von seinen WpfSettings verwendet. Geändert, aber verwendet. Dies ist in der Dokumentation verlinkt.

derskythe   
[![N|Solid](https://avatars.githubusercontent.com/u/913534?s=60&v=4)](https://github.com/derskythe/WpfSettings/)

#### I'm sorry
The source code is written in English. But the documentation is translated in
from German to English. I'm sorry. 😉 I hope you can
understand the documentation. If you have any questions, please contact me.