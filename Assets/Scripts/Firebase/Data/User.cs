using UnityEngine;

public class User
{
    public string Nickname;
    public int Wins;
    public int Loses;

    public User(string nick)
    {
        Nickname = nick;
        Wins = 0;
        Loses = 0;
    }
}
