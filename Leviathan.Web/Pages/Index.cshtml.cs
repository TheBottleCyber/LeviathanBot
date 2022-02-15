using System;
using System.Globalization;
using System.Web;
using ESI.NET;
using ESI.NET.Enumerations;
using Leviathan.Core.DatabaseContext;
using Leviathan.Core.Localization;
using Leviathan.Core.Models.Database;
using Leviathan.Core.Models.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Leviathan.Web.Pages
{
    public class IndexModel : PageModel
    {
        public readonly string _eveButtonLink;
        public readonly string _discordButtonLink;
        public readonly string _state;

        public readonly string _localizedIndexInstructionHeader;
        public readonly string[] _localizedIndexInstructionText;

        public IndexModel(MemoryContext context, Settings settings)
        {
            // temporary?
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(settings.BotConfig.Language);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(settings.BotConfig.Language);
            
            _localizedIndexInstructionHeader = LocalizationHelper.GetLocalizedString("WebInstructionsHeader");
            _localizedIndexInstructionText = LocalizationHelper.GetLocalizedString("WebInstructionsText")
                                                               .Split("~-~"); // that cant split normally
            
            var esiClient = new EsiClient(settings.ESIConfig);
            
            _state = Guid.NewGuid().ToString().Replace("-", "")[..16];
            
            context.Characters.Add(new Character(_state));
            context.SaveChanges();
            
            _eveButtonLink = esiClient.SSO.CreateAuthenticationUrl(state: _state);
            _discordButtonLink = $"https://discord.com/api/oauth2/authorize?client_id={settings.DiscordConfig.ClientId}&" +
                                 $"redirect_uri={settings.DiscordConfig.CallbackUrl}&" +
                                 "response_type=code&" +
                                 "scope=identify&" +
                                 "state=" + _state;
        }

        public void OnGet() { }
    }
}