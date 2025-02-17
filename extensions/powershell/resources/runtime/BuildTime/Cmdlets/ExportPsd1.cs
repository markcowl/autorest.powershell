﻿using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using static Microsoft.Rest.ClientRuntime.PowerShell.PsHelpers;

namespace Microsoft.Rest.ClientRuntime.PowerShell
{
    [Cmdlet(VerbsData.Export, "Psd1")]
    [DoNotExport]
    public class ExportPsd1 : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string ExportsFolder { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string CustomFolder { get; set; }

        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Psd1Path { get; set; }

        private static readonly bool IsAzure = Convert.ToBoolean(@"${$project.azure}");
        private const string CustomFolderRelative = "${$project.customFolder}";
        private const string Indent = "  ";
        private const string GuidStart = Indent + "GUID";

        protected override void ProcessRecord()
        {
            if (!Directory.Exists(ExportsFolder))
            {
                throw new ArgumentException($"Exports folder '{ExportsFolder}' does not exist");
            }

            if (!Directory.Exists(CustomFolder))
            {
                throw new ArgumentException($"Custom folder '{CustomFolder}' does not exist");
            }

            var guid = Guid.NewGuid();
            if (File.Exists(Psd1Path))
            {
                var currentGuid = File.ReadAllLines(Psd1Path)
                    .FirstOrDefault(l => l.StartsWith(GuidStart))?.Split(new[] {" = "}, StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault()?.Replace("'", String.Empty);
                guid = currentGuid != null ? Guid.Parse(currentGuid) : guid;
            }

            var sb = new StringBuilder();
            sb.AppendLine("@{");
            sb.AppendLine($@"{GuidStart} = '{guid}'");
            sb.AppendLine($@"{Indent}RootModule = '{"${$project.psm1}"}'");
            sb.AppendLine($@"{Indent}ModuleVersion = '{"${$project.moduleVersion}"}'");
            sb.AppendLine($@"{Indent}CompatiblePSEditions = 'Core', 'Desktop'");
            var author = IsAzure ? "Microsoft Corporation" : "${$project.metadata.authors}";
            sb.AppendLine($@"{Indent}Author = '{author}'");
            var companyName = IsAzure ? "Microsoft Corporation" : "${$project.metadata.companyName}";
            sb.AppendLine($@"{Indent}CompanyName = '{companyName}'");
            var copyright = IsAzure ? "Microsoft Corporation. All rights reserved." : "${$project.metadata.copyright}";
            sb.AppendLine($@"{Indent}Copyright = '{copyright}'");
            var description = IsAzure ? "Microsoft Azure PowerShell: ${$project.serviceName} cmdlets" : "${$project.metadata.description}";
            sb.AppendLine($@"{Indent}Description = '{description}'");
            sb.AppendLine($@"{Indent}PowerShellVersion = '5.1'");
            sb.AppendLine($@"{Indent}DotNetFrameworkVersion = '4.7.2'");
            sb.AppendLine($@"{Indent}RequiredAssemblies = '{"${$project.dll}"}'");

            var customFormatPs1xmlFiles = Directory.GetFiles(CustomFolder)
                .Where(f => f.EndsWith(".format.ps1xml"))
                .Select(f => $"{CustomFolderRelative}/{Path.GetFileName(f)}");
            var formatList = customFormatPs1xmlFiles.Prepend("${$project.formatPs1xml}").ToPsList();
            sb.AppendLine($@"{Indent}FormatsToProcess = {formatList}");

            var functionInfos = GetScriptCmdlets(ExportsFolder).ToArray();
            var cmdletsList = functionInfos.Select(sc => sc.Name).Distinct().Append("*").ToPsList();
            sb.AppendLine($@"{Indent}CmdletsToExport = {cmdletsList}");
            var aliasesList = functionInfos.SelectMany(i => i.ScriptBlock.Attributes).ToAliasNames().Append("*").ToPsList();
            sb.AppendLine($@"{Indent}AliasesToExport = {aliasesList}");

            sb.AppendLine($@"{Indent}PrivateData = @{{");
            sb.AppendLine($@"{Indent}{Indent}PSData = @{{");

            var tagsList = IsAzure ? "'Azure', 'ResourceManager', 'ARM', '${$project.serviceName}'" : "''";
            sb.AppendLine($@"{Indent}{Indent}{Indent}Tags = {tagsList}");
            var licenseUri = IsAzure ? "https://aka.ms/azps-license" : "";
            sb.AppendLine($@"{Indent}{Indent}{Indent}LicenseUri = '{licenseUri}'");
            var projectUri = IsAzure ? "https://github.com/Azure/azure-powershell" : "";
            sb.AppendLine($@"{Indent}{Indent}{Indent}ProjectUri = '{projectUri}'");
            sb.AppendLine($@"{Indent}{Indent}{Indent}ReleaseNotes = ''");
            var profilesList = "${$project.profiles.map(each => `'` + each + `'`).join(', ')}";
            if (IsAzure && !String.IsNullOrEmpty(profilesList))
            {
                sb.AppendLine($@"{Indent}{Indent}{Indent}Profiles = {profilesList}");
            }

            sb.AppendLine($@"{Indent}{Indent}}}");
            sb.AppendLine($@"{Indent}}}");
            sb.AppendLine(@"}");

            File.WriteAllText(Psd1Path, sb.ToString());
        }
    }
}
