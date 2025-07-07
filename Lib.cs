using SpacetimeDB;

public static partial class Module
{


    [Table(Name = "Room", Public = true)]
    public partial class Room
    {

        [PrimaryKey]
        [AutoInc]
        public int RoomID;
        public uint PlayersInRoom;
        public bool GameStarted;
        public Identity CurrentPlayerTurn;
        public int TimeLeft;


    }

    [Table(Name = "Player", Public = true)]
    public partial class Player
    {
        [PrimaryKey]
        public Identity identity;
        public string? UserName;
        public bool Online;
        public int HP;

        public float[] Location = [0, 0, 0];
        public float[] Rotation = [0, 0, 0];

    }


    [Reducer(ReducerKind.ClientConnected)]
    public static void PlayerConnected(ReducerContext ctx)
    {


        var player = ctx.Db.Player.identity.Find(ctx.Sender);


        if (player is not null) // this means the player actually exists
        {
            player.Online = true; // make the player online
            ctx.Db.Player.identity.Update(player);

        }
        else
        {
            ctx.Db.Player.Insert(new Player
            {
                identity = ctx.Identity

            });
        }
    }

    [Reducer(ReducerKind.ClientDisconnected)]
    public static void PlayerDisconnected(ReducerContext ctx)
    {

        var player = ctx.Db.Player.identity.Find(ctx.Sender);


        if (player is not null) // this means the player actually exists
        {
            player.Online = false; // make the player goes offline
            ctx.Db.Player.identity.Update(player);

        }

    }


    [Reducer]
    public static void PlayerChangedName(ReducerContext ctx, string NewName)
    {
        
            var player = ctx.Db.Player.identity.Find(ctx.Sender);


        if (player is not null) // this means the player actually exists
        {
            player.UserName = NewName; // make the player online
            ctx.Db.Player.identity.Update(player);

        }
    }

}