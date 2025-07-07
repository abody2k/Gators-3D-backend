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
        public bool GameStarted = false;
        public Identity? CurrentPlayerTurn;
        public Player[] Players = {};
        public int TimeLeft;
        public byte ActionsRemained;
        public byte[] votes = {};


    }

    [Table(Name = "Player", Public = true)]
    public partial class Player
    {
        [PrimaryKey]
        public Identity identity;
        public string? UserName;
        public bool Online;
        public int HP = 100;

        public int[] Location = [0, 0, 0];
        public byte Rotation; // reflects the number by 90 degress basically Rotation * 90 

        public override bool Equals(object? obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            // TODO: write your implementation of Equals() here
            return this.identity == (obj as Player)?.identity;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            return base.GetHashCode();
        }


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
                var newRoom = new Player[room.Players.Length + 1];
                for (int i = 0; i < newRoom.Length - 1; i++)
                {
                    newRoom[i] = room.Players[i];
                }
                newRoom[^1] = player;

                room.Players = newRoom;

                //start the game when there are 4 players

                if (newRoom.Length == 4)
                {
                    room.GameStarted = true;
                }
                else if (newRoom.Length == 1)
                {
                    room.CurrentPlayerTurn = player.identity;
                }

                ctx.Db.Room.RoomID.Update(room);



            }

        }

    }


    [Reducer]
    public static void MoveRotateAttack(ReducerContext ctx, int roomID, byte action, int[] direction)
    {


        // check if the player and room exist
        var room = RoomExist(roomID, ctx);
        var player = PlayerExist(ctx);

        if (room is not null && player is not null)
        {
            //check if it's the current player turn

            //check if the player has more actions to do
            if (room.CurrentPlayerTurn == player.identity && room.ActionsRemained > 0 && room.Players.Contains(player))
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

                        player.Rotation += (byte)Math.Clamp(direction[0], -1, 1);
                        player.Rotation %= 4;
                        break;
                    case 2: // attack
                            //check all the players and see if any of them exists in that place

                        var attackedPlayer = GetPlayerInThisLocation(player.Location, player.Rotation, room.Players);
                        if (attackedPlayer is not null) // it's not an empty block, there is a player over there
                        {
                            attackedPlayer.HP -= 10; // the damage can be updated in the future 
                            room.Players[Array.IndexOf(room.Players, attackedPlayer)] = attackedPlayer;
                        }
                        //if so then inflict damage on that player


                        break;
                    case 3: // do nothing
                        break;
                }

                if (room.ActionsRemained == 0) // player used all their actions
                {

                    var index = Array.IndexOf(room.Players, player) + 2;
                    index = index >= room.Players.Length ? 0 : index;
                    room.CurrentPlayerTurn = room.Players[index].identity; // change the current turn to the next player
                    room.ActionsRemained = 3; // reset the number of actions
                }

                //update the player that got hit
                room.Players[Array.IndexOf(room.Players, player)] = player;
                ctx.Db.Player.identity.Update(player);
                ctx.Db.Room.RoomID.Update(room);

                //

            }

        }




    }



    private static Room? RoomExist(int roomID, ReducerContext context)
    {


        return context.Db.Room.RoomID.Find(roomID);
    }

    private static Player? PlayerExist(ReducerContext context)
    {


        return context.Db.Player.identity.Find(context.Sender);
    }

    private static Player? GetPlayerInThisLocation(int[] location, byte rotation, Player[] players)
    {

        return players.First(p => p.Location == Location(rotation, location));

    }


    private static int[] Location(byte rotation, int[] location)// this function should reflects a specific gun but right now
    // the normal behavior is for a rifle
    {

        switch (Math.Abs(rotation))
        {

            case 0:
                location[1] += 1;
                break;
            case 1:
                location[0] += 1;
                break;
            case 2:
                location[1] -= 1;
                break;
            case 3:
                location[0] -= 1;
                break;
        }

        return location;
    }




    [Reducer]
    public static void CreateRoom(ReducerContext ctx)
    {


        ctx.Db.Room.Insert(new Room
        {

        });
    }


    [Reducer]
    public static void VoteGameStart(ReducerContext ctx, int roomID)
    {
        var room = RoomExist(roomID, ctx);
        var player = PlayerExist(ctx);

        if (room is not null && player is not null) // do they all exist ?
        {

            if (room.Players.Contains(player)) // is this player part of this room ?
            {

                if (!room.votes.Contains((byte)Array.IndexOf(room.Players, player)))
                {
                    var newVotes = new byte[room.votes.Length + 1];
                    Array.Copy(room.votes, newVotes, room.votes.Length);
                    newVotes[^1] = (byte)Array.IndexOf(room.Players, player);
                    room.votes = newVotes;
                    if (room.votes.Length == room.Players.Length && room.Players.Length > 1) // the number of votes are equal to the number of players
                    {
                        room.GameStarted = true;

                    }
                    ctx.Db.Room.RoomID.Update(room);

                }
            }
        }
}

}