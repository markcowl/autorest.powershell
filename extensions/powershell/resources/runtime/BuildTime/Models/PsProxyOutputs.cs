﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using static Microsoft.Rest.ClientRuntime.PowerShell.PsProxyOutputExtensions;
using static Microsoft.Rest.ClientRuntime.PowerShell.PsProxyTypeExtensions;

namespace Microsoft.Rest.ClientRuntime.PowerShell
{
    internal class OutputTypeOutput
    {
        public PSTypeName[] OutputTypes { get; }

        public OutputTypeOutput(IEnumerable<PSTypeName> outputTypes)
        {
            OutputTypes = outputTypes.ToArray();
        }

        public override string ToString() => OutputTypes != null && OutputTypes.Any() ? $"[OutputType({OutputTypes.Select(ot => $"'{ot}'").JoinIgnoreEmpty(ItemSeparator)})]{Environment.NewLine}" : String.Empty;
    }

    internal class CmdletBindingOutput
    {
        public VariantGroup VariantGroup { get; }

        public CmdletBindingOutput(VariantGroup variantGroup)
        {
            VariantGroup = variantGroup;
        }

        public override string ToString()
        {
            var dpsText = VariantGroup.DefaultParameterSetName.IsValidDefaultParameterSetName() ? $"DefaultParameterSetName='{VariantGroup.DefaultParameterSetName}'" : String.Empty;
            var sspText = VariantGroup.SupportsShouldProcess ? $"SupportsShouldProcess{ItemSeparator}ConfirmImpact='Medium'" : String.Empty;
            var pbText = $"PositionalBinding={false.ToPsBool()}";
            var propertyText = new[] { dpsText, pbText, sspText }.JoinIgnoreEmpty(ItemSeparator);
            return $"[CmdletBinding({propertyText})]{Environment.NewLine}";
        }
    }

    internal class ParameterOutput
    {
        public Parameter Parameter { get; }
        public bool HasMultipleVariantsInVariantGroup { get; }
        public bool HasAllVariantsInParameterGroup { get; }

        public ParameterOutput(Parameter parameter, bool hasMultipleVariantsInVariantGroup, bool hasAllVariantsInParameterGroup)
        {
            Parameter = parameter;
            HasMultipleVariantsInVariantGroup = hasMultipleVariantsInVariantGroup;
            HasAllVariantsInParameterGroup = hasAllVariantsInParameterGroup;
        }

        public override string ToString()
        {
            var psnText = HasMultipleVariantsInVariantGroup && !HasAllVariantsInParameterGroup ? $"ParameterSetName='{Parameter.VariantName}'" : String.Empty;
            var positionText = Parameter.Position != null ? $"Position={Parameter.Position}" : String.Empty;
            var mandatoryText = Parameter.IsMandatory ? "Mandatory" : String.Empty;
            var dontShowText = Parameter.DontShow ? "DontShow" : String.Empty;
            var vfpText = Parameter.ValueFromPipeline ? "ValueFromPipeline" : String.Empty;
            var helpMessage = Parameter.HelpMessage.ToPsStringLiteral();
            var helpText = !String.IsNullOrEmpty(helpMessage) ? $"HelpMessage='{helpMessage}'" : String.Empty;
            var propertyText = new[] { psnText, positionText, mandatoryText, dontShowText, vfpText, helpText }.JoinIgnoreEmpty(ItemSeparator);
            return $"{Indent}[Parameter({propertyText})]{Environment.NewLine}";
        }
    }

    internal class AliasOutput
    {
        public string[] Aliases { get; }
        public bool IncludeIndent { get; }

        public AliasOutput(string[] aliases, bool includeIndent = false)
        {
            Aliases = aliases;
            IncludeIndent = includeIndent;
        }

        public override string ToString() => Aliases?.Any() ?? false ? $"{(IncludeIndent ? Indent : String.Empty)}[Alias({Aliases.Select(an => $"'{an}'").JoinIgnoreEmpty(ItemSeparator)})]{Environment.NewLine}" : String.Empty;
    }

    internal class ValidateNotNullOutput
    {
        public bool HasValidateNotNull { get; }

        public ValidateNotNullOutput(bool hasValidateNotNull)
        {
            HasValidateNotNull = hasValidateNotNull;
        }

        public override string ToString() => HasValidateNotNull ? $"{Indent}[ValidateNotNull()]{Environment.NewLine}" : String.Empty;
    }

    internal class ArgumentCompleterOutput
    {
        public bool HasArgumentCompleter { get; }
        public Type ParameterType { get; }

        public ArgumentCompleterOutput(ParameterGroup parameterGroup)
        {
            HasArgumentCompleter = parameterGroup.HasArgumentCompleter;
            ParameterType = parameterGroup.ParameterType;
        }

        public override string ToString() => HasArgumentCompleter ? $"{Indent}[ArgumentCompleter([{ParameterType.Unwrap().ToPsType()}])]{Environment.NewLine}" : String.Empty;
    }

    internal class ParameterTypeOutput
    {
        public Type ParameterType { get; }

        public ParameterTypeOutput(Type parameterType)
        {
            ParameterType = parameterType;
        }

        public override string ToString() => $"{Indent}[{ParameterType.ToPsType()}]{Environment.NewLine}";
    }

    internal class ParameterNameOutput
    {
        public string ParameterName { get; }
        public bool IsLast { get; }

        public ParameterNameOutput(string parameterName, bool isLast)
        {
            ParameterName = parameterName;
            IsLast = isLast;
        }

        public override string ToString() => $"{Indent}${{{ParameterName}}}{(IsLast ? String.Empty : $",{Environment.NewLine}")}{Environment.NewLine}";
    }

    internal class BeginOutput
    {
        public VariantGroup VariantGroup { get; }

        public BeginOutput(VariantGroup variantGroup)
        {
            VariantGroup = variantGroup;
        }

        public override string ToString() => $@"begin {{
{Indent}try {{
{Indent}{Indent}$outBuffer = $null
{Indent}{Indent}if ($PSBoundParameters.TryGetValue('OutBuffer', [ref]$outBuffer)) {{
{Indent}{Indent}{Indent}$PSBoundParameters['OutBuffer'] = 1
{Indent}{Indent}}}
{Indent}{Indent}$parameterSet = $PsCmdlet.ParameterSetName
{GetParameterSetToCmdletMapping()}
{Indent}{Indent}$wrappedCmd = $ExecutionContext.InvokeCommand.GetCommand(($mapping[$parameterSet]), [System.Management.Automation.CommandTypes]::Cmdlet)
{Indent}{Indent}$scriptCmd = {{& $wrappedCmd @PSBoundParameters}}
{Indent}{Indent}$steppablePipeline = $scriptCmd.GetSteppablePipeline($myInvocation.CommandOrigin)
{Indent}{Indent}$steppablePipeline.Begin($PSCmdlet)
{Indent}}} catch {{
{Indent}{Indent}throw
{Indent}}}
}}

";

        private string GetParameterSetToCmdletMapping()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Indent}{Indent}$mapping = @{{");
            foreach (var variant in VariantGroup.Variants)
            {
                sb.AppendLine($@"{Indent}{Indent}{Indent}{variant.VariantName} = '{variant.PrivateModuleName}\{variant.PrivateCmdletName}';");
            }
            sb.Append($"{Indent}{Indent}}}");
            return sb.ToString();
        }

    }

    internal class ProcessOutput
    {
        public override string ToString() => $@"process {{
{Indent}try {{
{Indent}{Indent}$steppablePipeline.Process($_)
{Indent}}} catch {{
{Indent}{Indent}throw
{Indent}}}
}}

";
    }

    internal class EndOutput
    {
        public override string ToString() => $@"end {{
{Indent}try {{
{Indent}{Indent}$steppablePipeline.End()
{Indent}}} catch {{
{Indent}{Indent}throw
{Indent}}}
}}
";
    }

    internal class HelpCommentOutput
    {
        public VariantGroup VariantGroup { get; }
        public Type[] Inputs { get; }
        public Type[] Outputs { get; }

        public HelpCommentOutput(VariantGroup variantGroup, Type[] inputs, Type[] outputs)
        {
            VariantGroup = variantGroup;
            Inputs = inputs;
            Outputs = outputs;
        }

        public override string ToString()
        {
            var inputs = String.Join(Environment.NewLine, Inputs.Select(t => $".Inputs{Environment.NewLine}{t.FullName}"));
            var inputsText = !String.IsNullOrEmpty(inputs) ? $"{Environment.NewLine}{inputs}" : String.Empty;
            var outputs = String.Join(Environment.NewLine, Outputs.Select(t => $".Outputs{Environment.NewLine}{t.FullName}"));
            var outputsText = !String.IsNullOrEmpty(outputs) ? $"{Environment.NewLine}{outputs}" : String.Empty;
            return $@"<#
.Synopsis
{VariantGroup.Description}
.Description
{VariantGroup.Description}
.Example
To view examples, please use the -Online parameter with Get-Help or navigate to: {VariantGroup.Link}{inputsText}{outputsText}
.Link
{VariantGroup.Link}
#>
";
        }
    }

    internal class ParameterHelpOutput
    {
        public string HelpMessage { get; }

        public ParameterHelpOutput(string helpMessage)
        {
            HelpMessage = helpMessage;
        }

        public override string ToString() => !String.IsNullOrEmpty(HelpMessage)
            ? HelpMessage.Split(new [] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Aggregate(String.Empty, (c, n) => c + $"{Indent}# {n}{Environment.NewLine}")
            : String.Empty;
    }

    internal class ProfileOutput
    {
        public string ProfileName { get; }

        public ProfileOutput(string profileName)
        {
            ProfileName = profileName;
        }

        public override string ToString() => ProfileName != NoProfiles ? $"[{typeof(ProfileAttribute).ToPsAttributeType()}('{ProfileName}')]{Environment.NewLine}" : String.Empty;
    }

    internal class DescriptionOutput
    {
        public string Description { get; }

        public DescriptionOutput(string description)
        {
            Description = description;
        }

        public override string ToString() => !String.IsNullOrEmpty(Description) ? $"[{typeof(DescriptionAttribute).ToPsAttributeType()}('{Description.ToPsStringLiteral()}')]{Environment.NewLine}" : String.Empty;
    }

    internal class ParameterCategoryOutput
    {
        public ParameterCategory Category { get; }

        public ParameterCategoryOutput(ParameterCategory category)
        {
            Category = category;
        }

        public override string ToString() => $"{Indent}[{typeof(CategoryAttribute).ToPsAttributeType()}('{Category}')]{Environment.NewLine}";
    }

    internal static class PsProxyOutputExtensions
    {
        public const string NoParameters = "__NoParameters";

        public const string AllParameterSets = "__AllParameterSets";

        public const string Indent = "    ";

        public const string ItemSeparator = ", ";

        public static string ToPsBool(this bool value) => $"${value.ToString().ToLowerInvariant()}";

        public static string ToPsType(this Type type)
        {
            var regex = new Regex(@"^(.*)`{1}\d+(.*)$");
            var match = regex.Match(type.ToString());
            return match.Success ? $"{match.Groups[1]}{match.Groups[2]}" : type.ToString();
        }

        public static string ToPsAttributeType(this Type type) => type.ToPsType().RemoveEnd("Attribute");

        // https://stackoverflow.com/a/5284606/294804
        private static string RemoveEnd(this string text, string suffix) => text.EndsWith(suffix) ? text.Substring(0, text.Length - suffix.Length) : text;

        public static string ToPsStringLiteral(this string value) => value?.Replace("'", "''")?.Replace("‘", "''")?.Replace("’", "''")?.Replace("<br>", " ")?.Replace("\r\n", " ")?.Replace("\n", " ") ?? String.Empty;

        public static string JoinIgnoreEmpty(this IEnumerable<string> values, string separator) => String.Join(separator, values?.Where(v => !String.IsNullOrEmpty(v)));

        public static OutputTypeOutput ToOutputTypeOutput(this IEnumerable<PSTypeName> outputTypes) => new OutputTypeOutput(outputTypes);

        public static CmdletBindingOutput ToCmdletBindingOutput(this VariantGroup variantGroup) => new CmdletBindingOutput(variantGroup);

        public static ParameterOutput ToParameterOutput(this Parameter parameter, bool hasMultipleVariantsInVariantGroup, bool hasAllVariantsInParameterGroup) => new ParameterOutput(parameter, hasMultipleVariantsInVariantGroup, hasAllVariantsInParameterGroup);

        public static AliasOutput ToAliasOutput(this string[] aliases, bool includeIndent = false) => new AliasOutput(aliases, includeIndent);

        public static ValidateNotNullOutput ToValidateNotNullOutput(this bool hasValidateNotNull) => new ValidateNotNullOutput(hasValidateNotNull);

        public static ArgumentCompleterOutput ToArgumentCompleterOutput(this ParameterGroup parameterGroup) => new ArgumentCompleterOutput(parameterGroup);

        public static ParameterTypeOutput ToParameterTypeOutput(this Type parameterType) => new ParameterTypeOutput(parameterType);

        public static ParameterNameOutput ToParameterNameOutput(this string parameterName, bool isLast) => new ParameterNameOutput(parameterName, isLast);

        public static BeginOutput ToBeginOutput(this VariantGroup variantGroup) => new BeginOutput(variantGroup);

        public static ProcessOutput ToProcessOutput(this VariantGroup variantGroup) => new ProcessOutput();

        public static EndOutput ToEndOutput(this VariantGroup variantGroup) => new EndOutput();

        public static HelpCommentOutput ToHelpCommentOutput(this VariantGroup variantGroup, Type[] inputs, Type[] outputs) => new HelpCommentOutput(variantGroup, inputs, outputs);

        public static ParameterHelpOutput ToParameterHelpOutput(this string helpMessage) => new ParameterHelpOutput(helpMessage);

        public static ProfileOutput ToProfileOutput(this string profileName) => new ProfileOutput(profileName);

        public static DescriptionOutput ToDescriptionOutput(this string description) => new DescriptionOutput(description);

        public static ParameterCategoryOutput ToParameterCategoryOutput(this ParameterCategory category) => new ParameterCategoryOutput(category);
    }
}
