using UnityEngine;
using System;

public class Tile : MonoBehaviour {

    public Color boardColor1, boardColor2, playerColor, aiColor, selectedColor;
    private Color defaultColor;
    
    public SpriteRenderer pieceRenderer;
    // order is from player king to ai king
    public Sprite[] pieceSprites;
    
    // used to convert a piece's value to it's sprite index
    private int[] pieceValues = {-10000, -27, -15, -10, -9, -3, 0, 3, 9, 10, 15, 27, 10000};

    private Posn posn;
    private Chess chess;
    
    // called when this tile is clicked
	void OnMouseDown()
    {
        chess.PosnClicked(posn);
    }

    // called as a constructor after instantiation
    public void Initialize(Posn posn, Chess chess)
    {
        this.posn = posn;
        this.chess = chess;
        defaultColor = (posn.x + posn.y) % 2 == 0 ? boardColor1 : boardColor2;
        GetComponent<SpriteRenderer>().color = defaultColor;
    }

    // shows the given piece on this tile
    public void SetPiece(int piece)
    {
        
        pieceRenderer.sprite = pieceSprites[Array.IndexOf<int>(pieceValues, piece)];
        pieceRenderer.color = piece < 0 ? playerColor : aiColor;
    }

    // changes the color of this tile based on whether it was selected or unselected
    public void Selected(bool selected)
    {
        GetComponent<SpriteRenderer>().color = selected ? selectedColor : defaultColor;
    }

}