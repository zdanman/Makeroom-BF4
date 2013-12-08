
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
//using System.Drawing;


using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;

namespace PRoConEvents {
	public class CMakeroom : PRoConPluginAPI, IPRoConPluginInterface
	{
		#region Variables and Constructors
		private string m_strHostName;
		private string m_strPort;
		private string m_strPRoConVersion;
		private DateTime m_dateKickOptionsSet;
		private DateTime m_dateOld;

		// User Settings
		//private List<string> m_ignoreTags; can't implement until DICE gets there stuff together
		private int m_iMinKickScore;
		private int m_iKickDelay;

		private enumBoolYesNo m_bDebugOn;
		private enumBoolYesNo m_bPluginActive;

		private string m_strCommandGivenMessage;
		private string m_strPrivateKickMessage;

		private List<KeyValuePair<string, int>> m_kickOptions;
		
		#endregion


		public CMakeroom()
		{
			this.m_dateOld = this.m_dateKickOptionsSet = DateTime.Now;

			this.m_kickOptions = new List<KeyValuePair<string, int>>();
			this.m_iMinKickScore = 1000;
			this.m_iKickDelay = 10;
			this.m_bDebugOn = enumBoolYesNo.Yes;
			this.m_bPluginActive = enumBoolYesNo.No;
			this.m_strCommandGivenMessage = "Lowest score player will be kicked to make room for a Clan Member";
			this.m_strPrivateKickMessage = "You will be kicked to make room for a Clan Member";
		}

		#region PluginSetup

		public string GetPluginName() {
			return "Makeroom";
		}

		public string GetPluginVersion() {
			return "0.3";
		}

		public string GetPluginAuthor() {
			return "Dan Caldwell (AceDev)";
		}

		public string GetPluginWebsite() {
			return "www.cidclan.net";
		}

		public string GetPluginDescription() {
			return @"        <h2>Description</h2>
	        <p>This plugin allows an admin the choose from three of the lowest score players to kick (ex: in order to make room for a clan mate).  Just type <b>!makeroom</b> then <b>#1</b>, <b>#2</b>, or <b>#3</b> to kick them.<br /><br />
	        Plugin originally developed by Dan Caldwell for the CiD Clan.
	        <br /><br />
	        <h2>Settings Summary</h2>
	        <br />
	        <ul>
	          <li><b>Safe Score</b><br />
	          <br />
	          No players will be kicked if they have reached this score - even if they are the lowest.  Example: if set to 10 then only the newjoins will be likely kicked.  If set to 50000 then all lowscores are subject to kick.<br />
	          <br /><br />
	          </li>
	          <li><b>Kick Delay</b><br />
	          <br />
	          Seconds between command being issued and player being kicked (Gives the player time to read the bye-bye message).  Setting this to zero means immediate kick.
	          <br /><br />
	          </li>
	          <li><b>Global Message When Kicking</b><br />
	          <br />
	          Message to the server when command executed.
	          <br /><br />
	          </li>
	          <li><b>Message To The Kicked</b><br />
	          <br />
			Message to those being kicked.
	          <br /><br />
	          </i>
	          <li><b>Debug On</b><br />
	          <br />
			Ignore.  No longer used but can be used in the script if needed if extending plugin.
	          <br /><br />
	          </li>
	        </ul>
	        <br><br>
	        <h3>Change Log</h3><br>
	        <h4>0.3</h4><br>
	        <ul>
	        <li>Fixed: Small not enough players bug.</li>
	        <li>Added: Kick Delay</li>
	        <li>Changed: Message to kicked method.</li>
	        </ul>
	        <h4>0.2</h4><br>
	        <ul>
	        <li>Initial Full Release</li>
	        </ul>
	        <h4>0.1</h4><br>
	        <ul>
	        <li>Developmental release.</li>
	        </ul>

	        <br />
		";
		}

		public List<CPluginVariable> GetDisplayPluginVariables()
		{
			List<CPluginVariable> r = new List<CPluginVariable>();

			//r.Add(new CPluginVariable("Plugin|Clan Tags to Ignore (See Plugin Description)", typeof(string), GetTagsString() ));
			r.Add(new CPluginVariable("Plugin|Safe Score", this.m_iMinKickScore.GetType(), this.m_iMinKickScore));
			r.Add(new CPluginVariable("Plugin|Kick Delay", this.m_iKickDelay.GetType(), this.m_iKickDelay));
			r.Add(new CPluginVariable("Display|Global Message When Kicking", this.m_strCommandGivenMessage.GetType(), this.m_strCommandGivenMessage));
			r.Add(new CPluginVariable("Display|Message To The Kicked", this.m_strPrivateKickMessage.GetType(), this.m_strPrivateKickMessage));
			r.Add(new CPluginVariable("Display|Debug On", typeof(enumBoolYesNo), this.m_bDebugOn));

			return r;

		}

		public List<CPluginVariable> GetPluginVariables()
		{
			List<CPluginVariable> r = new List<CPluginVariable>();

			//r.Add(new CPluginVariable("Clan Tags to Ignore (See Plugin Description)", typeof(string), GetTagsString() ));
			r.Add(new CPluginVariable("Safe Score", this.m_iMinKickScore.GetType(), this.m_iMinKickScore));
			r.Add(new CPluginVariable("Kick Delay", this.m_iKickDelay.GetType(), this.m_iKickDelay));
			r.Add(new CPluginVariable("Global Message When Kicking", this.m_strCommandGivenMessage.GetType(), this.m_strCommandGivenMessage));
			r.Add(new CPluginVariable("Message To The Kicked", this.m_strPrivateKickMessage.GetType(), this.m_strPrivateKickMessage));
			r.Add(new CPluginVariable("Debug On", typeof(enumBoolYesNo), this.m_bDebugOn));

			return r;

		}

		public void SetPluginVariable(string strVariable, string strValue)
		{
			int i=0;

			/*if (strVariable.CompareTo("Clan Tags to Ignore (See Plugin Description)") == 0)
			{
				string[] split = strValue.Split(',');
				this.m_ignoreTags = new List<string>(split);
			}*/

			if (strVariable.CompareTo("Safe Score") == 0 && int.TryParse(strValue, out i) == true)
			{
				this.m_iMinKickScore = i;
			}
			else if (strVariable.CompareTo("Kick Delay") == 0 && int.TryParse(strValue, out i) == true)
			{
				this.m_iKickDelay = i;
			}
			else if (strVariable.CompareTo("Debug On") == 0 && Enum.IsDefined(typeof(enumBoolYesNo), strValue) == true)
			{
				this.m_bDebugOn = (enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue);
			}
			else if (strVariable.CompareTo("Global Message When Kicking") == 0)
			{
				this.m_strCommandGivenMessage = strValue;
			}
			else if (strVariable.CompareTo("Message To The Kicked") == 0)
			{
				this.m_strPrivateKickMessage = strValue;
			}
		}

		#endregion

		private void StartMakeroomSystem()
		{ this.m_bPluginActive = enumBoolYesNo.Yes; }

		private void StopMakeroomSystem()
		{ this.m_bPluginActive = enumBoolYesNo.No; }

		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
		{
			this.m_strHostName = strHostName;
			this.m_strPort = strPort;
			this.m_strPRoConVersion = strPRoConVersion;
			this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerLeft", "OnPlayerJoin", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnLevelLoaded");
		}

		public void OnPluginEnable()
		{
			this.ExecuteCommand("procon.protected.pluginconsole.write", "^bCMakeroom ^2Enabled!");
			StartMakeroomSystem();
		}

		public void OnPluginDisable() {
			this.ExecuteCommand("procon.protected.pluginconsole.write", "^bCMakeroom ^1Disabled =(");
			StopMakeroomSystem();
		}

		public void OnGlobalChat(string speaker, string message) {
			ProcessChatMessage(speaker, message);
		}

		public void OnTeamChat(string speaker, string message, int teamId) {
			ProcessChatMessage(speaker, message);
		}

		public void OnSquadChat(string speaker, string message, int teamId, int squadId) {
			ProcessChatMessage(speaker, message);
		}

		public void OnRoundOver(int winningTeamId)
		{
			StopMakeroomSystem();
		}

		public void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal) {
			StartMakeroomSystem();
		}

		private bool KickOptionsStillValid()
		{ TimeSpan duration = DateTime.Now - this.m_dateKickOptionsSet; return (this.m_kickOptions.Count >= 3 && duration.TotalSeconds < 15); }

		private void ProcessChatMessage(string speaker, string message)
		{
			CPrivileges cpPlayerPrivs = this.GetAccountPrivileges(speaker);

			if (this.m_bPluginActive == enumBoolYesNo.Yes)
			{
				Match match = Regex.Match(message, @"#(\d)");
				if (match.Success)
				{
					// this.ExecuteCommand("procon.protected.pluginconsole.write", "vote received " + match.Groups[1].Value);
					KickPlayerOption(Convert.ToInt32(match.Groups[1].Value));
				}

				match = Regex.Match(message, @"^!makeroom");
				if (match.Success)
				{
					// this.ExecuteCommand("procon.protected.pluginconsole.write", "makeroom chat message received by " + speaker);
					if (cpPlayerPrivs.CanKickPlayers)
					{
						KickLowPlayer(speaker);
					}
					else
					{
						this.ExecuteCommand("procon.protected.send", "admin.say", "makeroom privileges denied...", "all");
					}
				}
			}
		}

		private void KickLowPlayer(string speaker)
		{
			if (this.FrostbitePlayerInfoList.Count < 4)
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", "Not enough players", "all");
				return;
			}

			// is the current list still recent.
			if ( KickOptionsStillValid() )
			{
				DisplayKickOptions(speaker);
				return;
			}

			// refresh list
			this.m_kickOptions.Clear();

			foreach( KeyValuePair<string,CPlayerInfo> player in this.FrostbitePlayerInfoList )
			{
				if (player.Value.Score < this.m_iMinKickScore)
				{
					// insert keeping top 3 on top (beginnning) - i know very primitive but it is the simplest/fastest way
					if (this.m_kickOptions.Count == 0 || player.Value.Score < this.m_kickOptions[0].Value)
						this.m_kickOptions.Insert(0, new KeyValuePair<string, int>(player.Key, player.Value.Score) );
					else if (this.m_kickOptions.Count == 1 || player.Value.Score < this.m_kickOptions[1].Value)
						this.m_kickOptions.Insert(1, new KeyValuePair<string, int>(player.Key, player.Value.Score) );
					else if (this.m_kickOptions.Count == 2 || player.Value.Score < this.m_kickOptions[2].Value)
						this.m_kickOptions.Insert(2, new KeyValuePair<string, int>(player.Key, player.Value.Score) );
				}
			}

			// begin execute
			if (this.m_kickOptions.Count > 0)
			{
				// display them
				DisplayKickOptions(speaker);
			}
			else
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", "No low score player found.", "all");
			}
		}


		private void DisplayKickOptions(string speaker)
		{
			if (this.m_kickOptions.Count < 3) // dummy proof it
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", "Not enough players", "all");
				return;
			}

			this.m_dateKickOptionsSet = DateTime.Now;

			// this.ExecuteCommand("procon.protected.pluginconsole.write", "OPTIONS DISPLAYED FOR " + speaker);

			this.ExecuteCommand("procon.protected.send", "admin.say", "Select Low Score Player To Kick:", "player", speaker);
			for(int i = 0; i < 3; i++)
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", '#' + i.ToString() + ' ' + this.m_kickOptions[i].Key, "player", speaker);
			}
		}

		private void KickPlayerOption(int option)
		{
			if (KickOptionsStillValid())
			{
				this.m_dateKickOptionsSet = this.m_dateOld; // invalidate kick options
				this.ExecuteCommand("procon.protected.send", "admin.say", this.m_strCommandGivenMessage, "all");
				this.ExecuteCommand("procon.protected.send", "admin.say", "Unfortunate Soldier: " + this.m_kickOptions[option].Key, "all");
				this.ExecuteCommand("procon.protected.send", "admin.say", this.m_kickOptions[option].Key + " - " + this.m_strPrivateKickMessage, "player", this.m_kickOptions[option].Key);
				this.ExecuteCommand("procon.protected.tasks.add", "CMakeroom", this.m_iKickDelay.ToString(), "1", "1", "procon.protected.send", "admin.kickPlayer", this.m_kickOptions[option].Key, null);
			}
		}

	}
}
