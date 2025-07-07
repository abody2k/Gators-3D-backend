using SpacetimeDB;

public static partial class Module
{


    [Table(Name = "Room", Public = true)]
    public partial class Room
    {

        [PrimaryKey]
        [AutoInc]
        public int RoomID;
        public byte PlayersInRoom;
        public bool GameStarted;
        public Identity CurrentPlayerTurn;
        public Identity[] Players = [];
        public int TimeLeft;
       public byte ActionsRemained;


    }

    [Table(Name = "Player", Public = true)]
    public partial class Player
    {
        [PrimaryKey]
        public Identity identity;
        public string? UserName;
        public bool Online;
        public int HP;

        public int[] Location = [0, 0, 0];
        public byte Rotation; // reflects the number by 90 degress basically Rotation * 90 

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
    public static void JoinRoom(ReducerContext ctx, int roomID)
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


    [Reducer] 
    public static void MoveRotateAttack(ReducerContext ctx, int roomID, byte action,  int[] direction)
    {


        // check if the player and room exist
        var room = RoomExist(roomID, ctx);
        var player = PlayerExist(ctx);

        if (room is not null && player is not null)
        {
            //check if it's the current player turn

            //check if the player has more actions to do
            if (room.CurrentPlayerTurn == player.identity && room.ActionsRemained > 0)
            {
                // reduce the actions that remains
                room.ActionsRemained--;


                switch (action)
                {

                    case 0: // move in the given direction
                     var newPosition = new int[3];
                    for (int i = 0; i < 3; i++)
                        {
                            newPosition[i] = player.Location[i] + Math.Clamp(direction[i], -1, 1);
                        }

                        player.Location = newPosition;
                        break;
                    case 1: // rotate

                    player.Rotation +=(byte) direction[0];
                    player.Rotation %= 4;
                        break;
                    case 2: // attack
                            //check all the players and see if any of them exists in that place
                            //if so then inflict damage on that player
                    
                    
                        break;
                    case 3: // do nothing
                        break;
                }

                //

            }

        }




    }



    private static Room? RoomExist(int roomID, ReducerContext context)
    {


        return context.Db.Room.RoomID.Find(roomID) ;
    }

    private static Player? PlayerExist( ReducerContext context)
    {


        return context.Db.Player.identity.Find(context.Sender) ;
    }
    


}