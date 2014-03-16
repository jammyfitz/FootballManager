using FootballManager.FootballDataSetTableAdapters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManager
{ 
    public static class FootballHelper
    {
        public static ObservableCollection<string> GetTeams(DataTable team)
        {
            ObservableCollection<string> _teams = new ObservableCollection<string>();

            foreach (DataRowView row in team.DefaultView)
            {
                _teams.Add(row["TeamName"].ToString());
            }

            return _teams;
        }

        public static ObservableCollection<string> GetPlayers(DataTable player)
        {
            ObservableCollection<string> _players = new ObservableCollection<string>();

            foreach (DataRowView row in player.DefaultView)
            {
                _players.Add(row["PlayerName"].ToString());
            }

            return _players;
        }

        public static ObservableCollection<PlayerMatch> GetPlayerMatches(FootballDataSet footballDataSet, int matchId)
        {
            ObservableCollection<PlayerMatch> _playerMatches = new ObservableCollection<PlayerMatch>();

            foreach (DataRowView row in footballDataSet.PlayerMatch.DefaultView)
            {
                PlayerMatch tempPlayerMatch = new PlayerMatch();
                tempPlayerMatch.PlayerID = int.Parse(row["PlayerID"].ToString());
                tempPlayerMatch.PlayerName = GetPlayerNameFromID(footballDataSet.Player, tempPlayerMatch.PlayerID);
                tempPlayerMatch.TeamID = int.Parse(row["TeamID"].ToString());
                tempPlayerMatch.TeamName = GetTeamNameFromID(footballDataSet.Team, tempPlayerMatch.TeamID);
                tempPlayerMatch.MatchID = int.Parse(row["MatchID"].ToString());

                if (matchId == tempPlayerMatch.MatchID || matchId == -1)
                    _playerMatches.Add(tempPlayerMatch);
            }

            return _playerMatches;
        }

        public static List<Match> GetRelevantMatches(FootballDataSet.MatchDataTable match)
        {
            List<Match> allMatches = CompleteMatchData(match);
            List<Match> relevantMatches = RemoveMatchesOlderThanAMonth(allMatches);

            return relevantMatches;
        }

        private static List<Match> CompleteMatchData(FootballDataSet.MatchDataTable match)
        {
            List<Match> allMatches = new List<Match>();

            foreach (FootballDataSet.MatchRow row in match)
            {
                Match tempMatch = new Match();
                tempMatch.MatchID = row.MatchID;
                tempMatch.MatchDate = Utils.ChangeDateFormatToYYYYMMDD(row.MatchDate);
                tempMatch.MatchWinner = GetValueForMatchWinner(row);
                allMatches.Add(tempMatch);
            }

            return allMatches;
        }

        public static string GetPlayerNameFromID(FootballDataSet.PlayerDataTable player, int playerID)
        {
            return player.FindByPlayerID(playerID).PlayerName.ToString();
        }

        public static string GetTeamNameFromID(FootballDataSet.TeamDataTable team, int teamID)
        {
            return team.FindByTeamID(teamID).TeamName.ToString();
        }

        public static int GetMatchWinnerIndexByMatchID(FootballDataSet.MatchDataTable match, int matchId)
        {
            int indexToReturn = -1;
            DataRow matchRow = match.FindByMatchID(matchId);

            if (string.IsNullOrEmpty(matchRow["MatchWinner"].ToString()))
                return indexToReturn;

            indexToReturn = int.Parse(matchRow["MatchWinner"].ToString());
            return indexToReturn - 1;
        }

        public static List<Match> RemoveMatchesOlderThanAMonth(List<Match> matches)
        {
            List<Match> relevantMatches = new List<Match>();

            foreach (Match match in matches)
            {
                if (!Utils.IsOlderThanAMonth(match.MatchDate))
                    relevantMatches.Add(match);
            }
            return relevantMatches;
        }

        public static int GetDefaultMatchId(FootballDataSet.MatchDataTable match)
        {
            return match.First().MatchID;
        }

        public static string GetFirstTeamName(FootballDataSet.TeamDataTable team)
        {
            return team.First().TeamName;
        }

        public static string GetLastTeamName(FootballDataSet.TeamDataTable team)
        {
            return team.Last().TeamName;
        }

        public static void DeletePlayerMatches(PlayerMatchTableAdapter taPlayerMatches, int matchID)
        {
            taPlayerMatches.Adapter.DeleteCommand.CommandText = "DELETE FROM [dbo].[PlayerMatch] WHERE ([MatchID] = @Original_MatchID)";
            taPlayerMatches.Adapter.DeleteCommand.Parameters.Clear();
            taPlayerMatches.Adapter.DeleteCommand.Parameters.Add(new global::System.Data.SqlClient.SqlParameter("@Original_MatchID", global::System.Data.SqlDbType.Int, 0, global::System.Data.ParameterDirection.Input, 0, 0, "MatchID", global::System.Data.DataRowVersion.Original, false, null, "", "", ""));
            taPlayerMatches.Adapter.DeleteCommand.Parameters[0].Value = ((int)(matchID));
            taPlayerMatches.Adapter.DeleteCommand.Connection.Open();

            try
            {
                taPlayerMatches.Adapter.DeleteCommand.ExecuteNonQuery();
            }
            finally
            {
                taPlayerMatches.Adapter.DeleteCommand.Connection.Close();
            }
        }

        public static bool PlayerMatchCompletedByUser(PlayerMatch playerMatch)
        {
            if (!string.IsNullOrEmpty(playerMatch.PlayerName) && !string.IsNullOrEmpty(playerMatch.TeamName))
                return true;

            return false;
        }

        public static int GetPlayerIDFromName(DataTable player, string playerName)
        {
            DataRow playerRow = player.Select("PlayerName = '" + playerName + "'").Single();
            return int.Parse(playerRow["PlayerID"].ToString());
        }

        public static int GetTeamIDFromName(DataTable team, string teamName)
        {
            DataRow teamRow = team.Select("TeamName =  '" + teamName + "'").Single();
            return int.Parse(teamRow["TeamID"].ToString());
        }

        public static int GetValueForMatchWinner(FootballDataSet.MatchRow match)
        {
            if (match.IsMatchWinnerNull())
                return -1;

            return match.MatchWinner;
        }

        public static string FormatStatsDataToText(DataView playerStats)
        {
            string formattedData = "***MoleInTheBarn v1.0***\n";

            foreach (DataRowView playerStat in playerStats)
            {
                string playerName = playerStat["PlayerName"].ToString();
                string numberOfWins = playerStat["MatchWins"].ToString();

                formattedData += WritePlayerStatLine(playerName, numberOfWins);
            }

            return formattedData;
        }

        public static string WritePlayerStatLine(string playerName, string numberOfWins)
        {
            return string.Format("{0} : {1}\n", playerName, numberOfWins);
        }

        public static bool RowHasTeamButNoPlayer(PlayerMatch gridRow)
        {
            return string.IsNullOrEmpty(gridRow.PlayerName) && !string.IsNullOrEmpty(gridRow.TeamName);
        }

        public static bool RowHasPlayerButNoTeam(PlayerMatch gridRow)
        {
            return string.IsNullOrEmpty(gridRow.TeamName) && !string.IsNullOrEmpty(gridRow.PlayerName);
        }
    }
}
