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
        public Identity[] Players = [];
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


    [Reducer]
    public static void JoinRoom(ReducerContext ctx,int roomID)
    {
        
        var player = ctx.Db.Player.identity.Find(ctx.Sender);
        var room = ctx.Db.Room.RoomID.Find(roomID);

        if (player is not null && room is not null) // this means the player actually exists and there is such a room
        {
            if (room.GameStarted)
            {
                // no one can join
                return;
            }
            else
            {
                var newRoom = new Identity[room.Players.Length + 1];
                for (int i = 0; i < newRoom.Length - 1; i++)
                {
                    newRoom[i] = room.Players[i];
                }
                newRoom[^1] = ctx.Sender;

                room.Players = newRoom;

                //start the game when there are 4 players

                if (newRoom.Length == 4)
                {
                    room.GameStarted = true;
                }

                ctx.Db.Room.RoomID.Update(room);

                
                
            }

        }

    }
}