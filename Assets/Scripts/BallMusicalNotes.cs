using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallMusicalNotes : MonoBehaviour
{
    public int[] scale = new int[] { 36, 42, 34, 40, 43, 50 };
    public bool addOctaveUp = false;
    public bool addOctaveDown = false;

    private int noteIndex = 0;
    private List<int> allNotes = new List<int>();

    // Use this for initialization
    void Start()
    {
        foreach (int note in scale)
        {
            allNotes.Add(note);
            if (addOctaveUp) allNotes.Add(note + 12);
            if (addOctaveDown) allNotes.Add(note - 12);
        }
        ShuffleNotes();
    }

    private void ShuffleNotes()
    {
        allNotes = allNotes.OrderBy(x => Random.value).ToList();
    }

    public int GetNextNote()
    {
        int nextNote = 0;
        if (noteIndex < allNotes.Count)
        {
            nextNote = allNotes[noteIndex];
            noteIndex++;
        }
        else
        {
            noteIndex = 0;
        }
        return nextNote;
    }
}
