using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Net;
using System.Web;
using System.Data;
using System.Threading;
using System.Timers;
using System.Diagnostics;
using System.Reflection;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Players.Items;
using PRoCon.Core.Battlemap;
using PRoCon.Core.Maps;


namespace PRoConEvents
{

//Aliases
using EventType = PRoCon.Core.Events.EventType;
using CapturableEvent = PRoCon.Core.Events.CapturableEvents;

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

		private bool m_bDebug;
		private bool m_bPluginActive;
		private bool m_bPluginEnabled;

		private string m_strCommandGivenMessage;
		private string m_strPrivateKickMessage;

		private List<KeyValuePair<string, int>> m_kickOptions;
		protected Dictionary<string, CPlayerInfo> PlayerList;
		private string m_strAdmin;
		
		#endregion


		public CMakeroom()
		{
			this.m_dateOld = this.m_dateKickOptionsSet = DateTime.Now;

			this.m_kickOptions = new List<KeyValuePair<string, int>>();
			this.PlayerList = new Dictionary<string, CPlayerInfo>();
			this.m_iMinKickScore = 1000;
			this.m_iKickDelay = 10;
			this.m_bDebug = false;
			this.m_bPluginActive = false;
			this.m_strCommandGivenMessage = "Lowest score player will be kicked to make room for a Clan Member";
			this.m_strPrivateKickMessage = "You will be kicked to make room for a Clan Member";
			this.m_strAdmin = " ";
		}

		#region PluginSetup

		public string GetPluginName() {
			return "Makeroom";
		}

		public string GetPluginVersion() {
			return "0.7.0.1";
		}

		public string GetPluginAuthor() {
			return "Dan Caldwell (AceDev)";
		}

		public string GetPluginWebsite() {
			return "www.cidclan.net";
		}

		public string GetPluginDescription() {
			return @"        <h2>Description</h2>
	        <p>This plugin allows an admin the choose from three of the lowest score players to kick (ex: in order to make room for a clan mate).
	        <br><br>
		   Just type <b>!makeroom</b> then <b>#1</b>, <b>#2</b>, or <b>#3</b> to kick them.
		   <br><br>
		   Type <b>?makeroom</b> to kick the lowest score player without prompt.
		   <br /><br />
	        Plugin originally developed by Dan Caldwell for the CiD Clan.
	        <br /><br />
	        <h2>Settings Summary</h2>
	        <br />
	        <ul>
	          <li><b>Safe Score</b><br />
	          <br />
	          No players will be kicked if they have reached this score - even if they are the lowest.  Example: if set to 10 then only the newjoins will be likely kicked.  If set to 50000 then all lowscores are subject to kick.  (Default:1000)<br />
	          <br /><br />
	          </li>
	          <li><b>Kick Delay</b><br />
	          <br />
	          Seconds between command being issued and player being kicked (Gives the player time to read the bye-bye message).  Setting this to zero means immediate kick. (Default: 10)
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
			Display debug messages in procon console window (only useful for developers).
	          <br /><br />
	          </li>
	        </ul>
	        <br><br>
	        <h3>Change Log</h3><br>
	        <h4>0.7</h4><br>
	        <ul>
	        <li>Fixed: Player not listing bugs.</li>
		   <li>Fixed: Various improvements and compatibility changes.</li>
	        </ul>
	        <h4>0.6</h4><br>
	        <ul>
	        <li>Fixed: Privilege Denied message is now sent to player who executed.</li>
	        <li>Changed: User friendly ZIP file.</li>
	        </ul>
	        <h4>0.5</h4><br>
	        <ul>
	        <li>Fixed: Made several improvements/fixes.</li>
	        </ul>
	        <h4>0.4</h4><br>
	        <ul>
	        <li>Fixed: Several bugs.</li>
	        <li>Added: ?makeroom command</li>
		   <li>Added: one admin at a time - procedure.</li>
	        </ul>
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
			r.Add(new CPluginVariable("Display|Debug On", typeof(enumBoolYesNo), (this.m_bDebug ? enumBoolYesNo.Yes : enumBoolYesNo.No) ));

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
			r.Add(new CPluginVariable("Debug On", typeof(enumBoolYesNo), (this.m_bDebug ? enumBoolYesNo.Yes : enumBoolYesNo.No) ));

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
				if ( ((enumBoolYesNo)Enum.Parse(typeof(enumBoolYesNo), strValue)) == enumBoolYesNo.Yes)
					this.m_bDebug = true;
				else
					this.m_bDebug = false;
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

		public void WriteDebugConsole(string output)
		{
			if (this.m_bDebug) this.ExecuteCommand("procon.protected.pluginconsole.write", "^bCMakeroom: " + output);
		}

		private void StartMakeroomSystem()
		{ if (this.m_bPluginEnabled) this.m_bPluginActive = true; }

		private void StopMakeroomSystem()
		{ this.m_bPluginActive = false; }

		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
		{
			this.m_strHostName = strHostName;
			this.m_strPort = strPort;
			this.m_strPRoConVersion = strPRoConVersion;
			this.RegisterEvents(this.GetType().Name, "OnListPlayers", "OnPlayerLeft", "OnPlayerJoin", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnLevelLoaded");
		}

          public void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
		{
	     	if (subset.Subset == CPlayerSubset.PlayerSubsetType.All)
			{
				this.PlayerList.Clear(); // fresh list
               	foreach (CPlayerInfo p in players)
				{
					this.PlayerList.Add(p.SoldierName, p);
				}
            	}
		}
		
		public void OnPlayerLeft(CPlayerInfo p)
		{
     		if ( this.PlayerList.ContainsKey(p.SoldierName) )
			{
				this.PlayerList.Remove(p.SoldierName);
			}
		}
		
		public void OnPlayerJoin(string soldierName)
		{
			if (!this.PlayerList.ContainsKey(soldierName))
			{
				this.PlayerList.Add(soldierName, new CPlayerInfo(soldierName, "", 0, 24));
			}
		}

		public void OnPluginEnable()
		{
			WriteDebugConsole("^2Enabled!");
			this.m_bPluginEnabled = true;
			StartMakeroomSystem();
		}

		public void OnPluginDisable() {
			WriteDebugConsole("^1Disabled =(");
			this.m_bPluginEnabled = false;
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
			if (!this.m_bPluginActive) return;

			CPrivileges cpPlayerPrivs = this.GetAccountPrivileges(speaker);


			Match match = Regex.Match(message, @"#(\d)");
			if (match.Success)
			{
				if (cpPlayerPrivs.CanKickPlayers)
				{
					// kick player selected - 1 (because displayed is [1 - 3] not [0 - 2]
					KickPlayerOption(speaker, (Convert.ToInt32(match.Groups[1].Value)) - 1);
				}
			}

			match = Regex.Match(message, @"^!makeroom");
			if (match.Success)
			{
				WriteDebugConsole("makeroom chat message received by " + speaker);
				if (cpPlayerPrivs.CanKickPlayers)
				{
					if (KickOptionsStillValid() && speaker != this.m_strAdmin)
					{
						this.ExecuteCommand("procon.protected.send", "admin.say", "One admin at a time please!  Please wait 10 seconds and try again.", "player", speaker);
					}
					else
					{
						WriteDebugConsole("executing !makeroom");

						GenerateKickOptions(speaker);
						DisplayKickOptions(speaker);
					}
				}
				else
				{
					this.ExecuteCommand("procon.protected.send", "admin.say", "makeroom privileges denied...", "player", speaker);
				}
			}

			match = Regex.Match(message, @"^\?makeroom");
			if (match.Success)
			{
				WriteDebugConsole("force makeroom chat message received by " + speaker);
				if (cpPlayerPrivs.CanKickPlayers)
				{
					if (KickOptionsStillValid() && speaker != this.m_strAdmin)
					{
						this.ExecuteCommand("procon.protected.send", "admin.say", "One admin at a time please!  Please wait 10 seconds and try again.", "player", speaker);
					}
					else
					{
						WriteDebugConsole("executing ?makeroom");

						GenerateKickOptions(speaker);
						KickPlayerOption(speaker, 0); // go ahead and kick first player in list
					}
				}
				else
				{
					this.ExecuteCommand("procon.protected.send", "admin.say", "makeroom privileges denied...", "player", speaker);
				}
			}
		}

		private void GenerateKickOptions(string speaker)
		{
			if (this.PlayerList.Count < 3)
			{
				WriteDebugConsole(this.PlayerList.Count.ToString() + " players available - not enough in KickLowPlayer");
				this.ExecuteCommand("procon.protected.send", "admin.say", "Not enough players", "player", speaker);
				return;
			}

			// is the current list still recent.
			if ( KickOptionsStillValid() ) return;

			// refresh list
			this.m_kickOptions.Clear();

			foreach( KeyValuePair<string,CPlayerInfo> player in this.PlayerList )
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
			
			// check list
			if (this.m_kickOptions.Count == 0)
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", "No low score player found.", "player", speaker);
				return;
			}
			if (this.m_kickOptions.Count < 3)
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", "Not enough players.", "player", speaker);
				return;
			}

			// done - validate list
			this.m_dateKickOptionsSet = DateTime.Now;
			this.m_strAdmin = speaker;
		}


		private void DisplayKickOptions(string speaker)
		{
			if (!KickOptionsStillValid()) return;

			this.ExecuteCommand("procon.protected.send", "admin.say", "Select Low Score Player To Kick:", "player", speaker);
			for(int i = 0; i < 3; i++)
			{
				WriteDebugConsole("displaying soldier option: #" + (i+1).ToString() + " " + this.m_kickOptions[i].Key);

				this.ExecuteCommand("procon.protected.send", "admin.say", '#' + (i+1).ToString() + ' ' + this.m_kickOptions[i].Key, "player", speaker);
			}
		}

		private void KickPlayerOption(string speaker, int option)
		{
			if (!KickOptionsStillValid())
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", "To long between commands.  Type !makeroom command again.", "player", speaker);
				return;
			}

			if (speaker != this.m_strAdmin)
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", "One admin at a time please!  Please wait 10 seconds and try again.", "player", speaker);
				return;
			}

			if (option < this.m_kickOptions.Count && option <= 2 && option >= 0)
			{
				WriteDebugConsole("kicking soldier " + this.m_kickOptions[option].Key);

				this.m_dateKickOptionsSet = this.m_dateOld; // invalidate kick options
				this.ExecuteCommand("procon.protected.send", "admin.say", this.m_strCommandGivenMessage, "all");
				this.ExecuteCommand("procon.protected.send", "admin.say", "Unfortunate Soldier: " + this.m_kickOptions[option].Key, "all");
				this.ExecuteCommand("procon.protected.send", "admin.say", this.m_kickOptions[option].Key + " - " + this.m_strPrivateKickMessage, "player", this.m_kickOptions[option].Key);
				this.ExecuteCommand("procon.protected.tasks.add", "CMakeroom", this.m_iKickDelay.ToString(), "1", "1", "procon.protected.send", "admin.kickPlayer", this.m_kickOptions[option].Key, this.m_strPrivateKickMessage);
				//this.PlayerList.Remove(this.m_kickOptions[option].Key); // force removal from list
			}
			else
			{
				this.ExecuteCommand("procon.protected.send", "admin.say", "Invalid player selection.", "player", speaker);
			}

		}

	}
}
