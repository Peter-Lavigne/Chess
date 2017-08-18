using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using uCPf;

// keeps track of game logic and chess rules
public class Chess : MonoBehaviour {

    /*
     * 
     *   BOARD SETUP
     * 
     * 
     */


    // piece values
    public const int EMPTY = 0;
    public const int PAWN = 3;
    public const int BISHOP = 9;
    public const int KNIGHT = 10;
    public const int ROOK = 15;
    public const int QUEEN = 27;
    public const int KING = 10000;

    // represents the board
    public int[,] board;

    public GameObject tilePrefab;
    private Tile[,] tiles;

    public ColorPicker playerColorPicker, aiColorPicker;
    private bool colorsNotUpdated;

    // sets up the game
    void Start()
    {
        Screen.fullScreen = false;
        Reset();
    }

    public void Reset()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        from = null;
        SetupBoard();
        CreateTiles();
        UpdateTiles();
        value = 0;
        ChangeDifficulty();
    }

    // creates the inital board
    void SetupBoard()
    {
        board = new int[8, 8];
        int[] boardSetup = { ROOK, KNIGHT, BISHOP, QUEEN, KING, BISHOP, KNIGHT, ROOK };
        for (int x = 0; x < 8; x++)
        {
            board[x, 1] = -PAWN;
            board[x, 6] = PAWN;
            board[x, 0] = -boardSetup[x];
            board[x, 7] = boardSetup[x];
        }
    }
    
    // creates the tiles
    void CreateTiles()
    {
        tiles = new Tile[8, 8];
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                GameObject tile = Instantiate<GameObject>(tilePrefab);
                tile.transform.SetParent(transform);
                tile.transform.localPosition = new Vector2(x, y);
                tiles[x, y] = tile.GetComponent<Tile>();
                Tile tileScript = tile.GetComponent<Tile>();
                tileScript.Initialize(new Posn(x, y), this);
                tileScript.aiColor = aiColorPicker.color;
                tileScript.playerColor = playerColorPicker.color;
                
            }
        }
    }

    // updates every tile to it's corresponding piece on the given board
    public void UpdateTiles()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                tiles[x, y].SetPiece(board[x, y]);
            }
        }
    }

    // updates the tile at the given posn
    public void UpdateTile(Posn posn)
    {
        tiles[posn.x, posn.y].SetPiece(GetPiece(posn));
    }

    // changes the color of a selected tile based on whether it was selected/unselected
    public void TileSelected(Posn posn, bool selected)
    {
        tiles[posn.x, posn.y].Selected(selected);
    }

    public void UpdateColors()
    {
        if (tiles == null)
        {
            return;
        }
        foreach (Tile tile in tiles)
        {
            tile.aiColor = aiColorPicker.color;
            tile.playerColor = playerColorPicker.color;
        }
        UpdateTiles();
    }

    /*
     * 
     *   GAME LOGIC
     * 
     * 
     */

    const int PLAYER = -1;
    const int NEUTRAL = 0;
    const int AI = 1;
    
    // the piece the player has selected to move.
    // the piece goes FROM here TO somewhere else
    private Posn from;

    // handles the given posn being clicked
    public void PosnClicked(Posn posn)
    {
        if (GameOver())
        {
            return;
        }

        if (from == null && WhatTeam(posn) == PLAYER)
        {
            TileSelected(posn, true);
            from = posn;
        }
        else if (from != null)
        {
            Move move = ValidMove(new Move(from, posn, GetPiece(posn)));
            if (move != null)
            {
                CommitMove(move);
                if (!GameOver())
                {
                    StartCoroutine(AITurn());
                }
            }
            TileSelected(from, false);
            from = null;
        }
    }

    // sets a move and updates it's tiles
    void CommitMove(Move move)
    {
        SetMove(move);
        UpdateTile(move.from);
        UpdateTile(move.to);
        if (move.HasSpecialRule() && move.rule.Equals("CASTLE"))
        {
            UpdateTiles();
        }
    }

    // if the given move is valid, returns that move,
    // otherwise returns null
    Move ValidMove(Move move)
    {
        foreach (Move m in MovesFromPosn(move.from))
        {
            if (move.Equals(m))
            {
                return m;
            }
        }
        return null;
    }

    // returns every move the given team can make
    List<Move> AllMoves(int team) {
        List<Move> moves = new List<Move>();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Posn from = new Posn(x, y);
                if (WhatTeam(from) == team)
                {
                    moves.AddRange(MovesFromPosn(from));
                }
            }
        }
        return moves;
    }

    // returns all the moves from the given posn
    List<Move> MovesFromPosn (Posn from)
    {
        int piece = Mathf.Abs(GetPiece(from));
        int team = WhatTeam(from);
        if (piece == EMPTY)
        {
            return new List<Move>();
        }
        else if (piece == PAWN)
        {
            return PawnMoves(from, team);
        }
        else if (piece == BISHOP)
        {
            return BishopMoves(from, team);
        }
        else if (piece == KNIGHT)
        {
            return KnightMoves(from, team);
        }
        else if (piece == ROOK)
        {
            return RookMoves(from, team);
        }
        else if (piece == QUEEN)
        {
            return QueenMoves(from, team);
        }
        else // piece == KING
        {
            return KingMoves(from, team);
        }
    }

    // changes the board state to reflect the given move being played
    public void SetMove(Move move)
    {
        if (move.HasSpecialRule())
        {
            if (move.rule.Equals("CASTLE"))
            {
                int y = WhatTeam(move.from) == PLAYER ? 0 : 7;
                SetPiece(move.to, GetPiece(move.from));
                SetPiece(move.from, EMPTY);
                Posn from, to;
                if (move.to.x < move.from.x)
                {
                    from = new Posn(0, y);
                    to = new Posn(3, y);
                }
                else
                {
                    from = new Posn(7, y);
                    to = new Posn(5, y);
                }
                SetPiece(to, GetPiece(from));
                SetPiece(from, EMPTY);
            }
            else // PROMOTION
            {
                int team = WhatTeam(move.from);
                SetPiece(move.to, QUEEN * team);
                SetPiece(move.from, EMPTY);
                value += (QUEEN - PAWN) * team;
                value -= move.removedPiece;
            }
        }
        else
        {
            int piece = GetPiece(move.from);
            SetPiece(move.from, EMPTY);
            SetPiece(move.to, piece);
            value -= move.removedPiece;
        }
    }

    // changes the board state to reflect the given move being undone
    public void UndoMove(Move move)
    {
        if (move.HasSpecialRule())
        {
            if (move.rule.Equals("CASTLE"))
            {
                int y = WhatTeam(move.to) == PLAYER ? 0 : 7;
                SetPiece(move.from, GetPiece(move.to));
                SetPiece(move.to, EMPTY);
                Posn from, to;
                if (move.to.x < move.from.x)
                {
                    from = new Posn(0, y);
                    to = new Posn(3, y);
                }
                else
                {
                    from = new Posn(7, y);
                    to = new Posn(5, y);
                }
                SetPiece(from, GetPiece(to));
                SetPiece(to, EMPTY);
            }
            else // PROMOTION
            {
                int team = WhatTeam(move.to);
                SetPiece(move.from, PAWN * team);
                SetPiece(move.to, move.removedPiece);
                value -= (QUEEN - PAWN) * team;
                value += move.removedPiece;
            }
        }
        else
        {
            SetPiece(move.from, GetPiece(move.to));
            SetPiece(move.to, move.removedPiece);
            value += move.removedPiece;
        }
    }

    /*
     * 
     * 
     *   BASIC HELPER METHODS
     * 
     * 
     */

    // returns which team a given posn belongs to
    public int WhatTeam(Posn posn)
    {
        return WhatTeam(GetPiece(posn));
    }

    // returns which team a given int belongs to
    public int WhatTeam(int piece)
    {
        return piece == 0 ? NEUTRAL : (piece < 0 ? PLAYER : AI);
    }

    // returns the piece at the given posn
    public int GetPiece(Posn posn)
    {
        return board[posn.x, posn.y];
    }

    // sets the given piece at the given posn
    public void SetPiece(Posn posn, int piece)
    {
        board[posn.x, posn.y] = piece;
    }

    // is the piece at the given posn an enemy of the given team?
    bool IsEnemy(Posn posn, int team)
    {
        return WhatTeam(posn) + team == 0;
    }

    // is there no piece at the given posn?
    bool IsEmpty(Posn posn)
    {
        return WhatTeam(posn) == NEUTRAL;
    }

    // is the given posn on the map?
    bool IsOnMap(Posn posn)
    {
        return posn.x >= 0 && posn.x < 8 && posn.y >= 0 && posn.y < 8;
    }

    // has someone won the game?
    public bool GameOver()
    {
        return Winner() != NEUTRAL;
    }

    // returns the current winner.
    // if a king has not been taken, NEUTRAL is returned
    public int Winner()
    {
        if (Mathf.Abs(value) > WINSCORE)
        {
            return value < 0 ? PLAYER : AI;
        }
        else
        {
            return NEUTRAL;
        }
    }


    /*
     * 
     * 
     *   PIECE MOVES
     * 
     * 
     */

    // a list of moves that the pawn at the given posn can make
    List<Move> PawnMoves(Posn from, int team)
    {
        int x = from.x;
        int y = from.y;
        List<Move> moves = new List<Move>();
        Posn to = new Posn(x, y - team);
        if (IsEmpty(to))
        {
            if (to.y % 7 == 0)
            {
                moves.Add(new Move(from, to, EMPTY, "PROMOTION"));
            }
            else
            {
                AddMoveIf(from, to, moves, true);
            }
            if ((y + team) % 7 == 0) {
                to = new Posn(x, y - team * 2);
                AddMoveIf(from, to, moves, IsEmpty(to));
            }
        }
        Posn to1 = new Posn(x - 1, y - team);
        Posn to2 = new Posn(x + 1, y - team);
        if ((y - team) % 7 == 0)
        {
            if (x != 0 && IsEnemy(to1, team))
            {
                moves.Add(new Move(from, to1, GetPiece(to1), "PROMOTION"));
            }
            if (x != 7 && IsEnemy(to2, team))
            {
                moves.Add(new Move(from, to2, GetPiece(to2), "PROMOTION"));
            }
        }
        else
        {
            AddMoveIf(from, to1, moves, x != 0 && IsEnemy(to1, team));
            AddMoveIf(from, to2, moves, x != 7 && IsEnemy(to2, team));
        }
        return moves;
    }

    //a list of moves that the knight at the given posn can make
    List<Move> KnightMoves(Posn from, int team)
    {
        List<Move> moves = new List<Move>();
        int x = from.x;
        int y = from.y;
        for (int i = -2; i <= 2; i++)
        {
            for (int j = 3 - Mathf.Abs(i); j > -3; j -= 2)
            {
                if (i == 0 || j == 0)
                    continue;
                Posn to = new Posn(x + i, y + j);
                AddMoveIf(from, to, moves, IsOnMap(to) && WhatTeam(to) != team);
            }
        }
        return moves;
    }

    //a list of moves that the bishop at the given posn can make
    List<Move> BishopMoves(Posn from, int team)
    {
        List<Move> moves = new List<Move>();
        AddMovesInDirection(1, 1, moves, from, team);
        AddMovesInDirection(1, -1, moves, from, team);
        AddMovesInDirection(-1, -1, moves, from, team);
        AddMovesInDirection(-1, 1, moves, from, team);
        return moves;
    }

    //a list of moves that the rook at the given posn can make
    List<Move> RookMoves(Posn from, int team)
    {
        List<Move> moves = new List<Move>();
        AddMovesInDirection(0, 1, moves, from, team);
        AddMovesInDirection(1, 0, moves, from, team);
        AddMovesInDirection(0, -1, moves, from, team);
        AddMovesInDirection(-1, 0, moves, from, team);
        return moves;
    }

    //a list of moves that the queen at the given posn can make
    List<Move> QueenMoves(Posn from, int team)
    {
        List<Move> moves = BishopMoves(from, team);
        moves.AddRange(RookMoves(from, team));
        return moves;
    }

    //a list of moves that the king at the given posn can make
    List<Move> KingMoves(Posn from, int team)
    {
        List<Move> moves = new List<Move>();
        int x = from.x;
        int y = from.y;
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                    continue;
                Posn to = new Posn(x + i, y + j);
                AddMoveIf(from, to, moves, (IsOnMap(to) && WhatTeam(to) != team));
            }
        }
        // castling
        if (x == 4 && y == (team == AI ? 7 : 0))
        {
            if (board[0, y] == ROOK * team
                && board[1, y] == EMPTY
                && board[2, y] == EMPTY
                && board[3, y] == EMPTY)
            {
                moves.Add(new Move(from, new Posn(x - 2, y), EMPTY, "CASTLE"));
            }
            if (board[7, y] == ROOK * team
                && board[6, y] == EMPTY
                && board[5, y] == EMPTY)
            {
                moves.Add(new Move(from, new Posn(x + 2, y), EMPTY, "CASTLE"));
            }
        }
        return moves;
    }

    // adds every empty/enemy space in the given (x,y) direction as a move
    void AddMovesInDirection(int xDir, int yDir, List<Move> moves, Posn from, int team)
    {
        Posn to = new Posn(from.x + xDir, from.y + yDir);
        while (IsOnMap(to))
        {
            if (!IsEmpty(to))
            {
                AddMoveIf(from, to, moves, IsEnemy(to, team));
                return;
            }
            AddMoveIf(from, to, moves, true);
            to = new Posn(to.x + xDir, to.y + yDir);
        }
    }

    // adds a move to moves if the given condition is true
    void AddMoveIf(Posn from, Posn to, List<Move> moves, bool condition)
    {
        if (condition)
        {
            moves.Add(new Move(from, to, GetPiece(to)));
        }
    }

    /*
     * 
     *   AI METHODS
     * 
     * 
     */

    //the value of the board
    int value;

    public Slider difficultySlider;
    private int turnLookahead;

    public float switchOdds;

    const int WINSCORE = 5000;
    const int MINALPHA = -5001;

    // changes the turn lookahead based on the difficulty slider
    public void ChangeDifficulty()
    {
        turnLookahead = (int)Mathf.Clamp(difficultySlider.value, 1, 5);
    }

    //handles the ai's turn
    public IEnumerator AITurn()
    {
        yield return new WaitForSeconds(0);
        CommitMove(GetAIMove());
    }
    
    // returns the next move for the AI
    public Move GetAIMove()
    {
        Move bestMove = null;
        int alpha = MINALPHA;
        foreach (Move move in AllMoves(AI))
        {
            SetMove(move);
            int score = GetMoveValue(move, AI, alpha, turnLookahead - 1);
            UndoMove(move);
            if (score > alpha || (score == alpha && Random.value < switchOdds))
            {
                alpha = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    // returns the heuristic value of the given move
    int GetMoveValue(Move move, int team, int alpha, int beta)
    {
        if (beta <= 0)
            return value;
        if (GameOver())
            return Winner() * WINSCORE;
        int enemyBestMove = MINALPHA * -team;
        foreach (Move nextMove in AllMoves(-team))
        {
            SetMove(nextMove);
            int score = GetMoveValue(nextMove, -team, alpha, beta - 1);
            UndoMove(nextMove);
            if (team == AI && score < alpha)
                return alpha - 1;
            enemyBestMove = team == AI ?
                Mathf.Min(enemyBestMove, score) :
                Mathf.Max(enemyBestMove, score);
        }
        return enemyBestMove;
    }
    
}

// posns represent board positions
public class Posn
{
    public int x;
    public int y;

    public Posn(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public bool Equals(Posn posn)
    {
        return this.x == posn.x && this.y == posn.y;
    }
}

// moves represent a piece moving FROM one posn TO another posn
public class Move
{
    public Posn from;
    public Posn to;

    // remembers the piece that will be destroyed if this move goes through
    public int removedPiece;

    // describes a special rule
    public string rule;

    public Move(Posn from, Posn to, int removedPiece)
    {
        this.from = from;
        this.to = to;
        this.removedPiece = removedPiece;
    }

    public Move(Posn from, Posn to, int removedPiece, string rule)
    {
        this.from = from;
        this.to = to;
        this.removedPiece = removedPiece;
        this.rule = rule;
    }

    // does this move contain a rule exception?
    public bool HasSpecialRule()
    {
        return rule != null;
    }

    public bool Equals(Move move)
    {
        return this.from.Equals(move.from) && this.to.Equals(move.to);
    }
}