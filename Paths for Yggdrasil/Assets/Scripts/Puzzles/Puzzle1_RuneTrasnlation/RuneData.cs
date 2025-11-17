using UnityEngine;
using System.Collections.Generic;

namespace Yggdrasil.Puzzles.RuneTranslation
{
    [System.Serializable]
    public class RunePair
    {
        public string latinChar;
        public Sprite runeSprite;
    }

    [CreateAssetMenu(fileName = "RuneData", menuPath = "Yggdrasil/Puzzles/Rune Data")]
    public class RuneData : ScriptableObject
    {
        public List<RunePair> alphabet;

        public Sprite GetRuneSprite(string latinChar)
        {
            RunePair pair = alphabet.Find(r => r.latinChar.ToUpper() == latinChar.ToUpper());
            return pair?.runeSprite;
        }

        public string GetLatinChar(Sprite runeSprite)
        {
            RunePair pair = alphabet.Find(r => r.runeSprite == runeSprite);
            return pair?.latinChar;
        }
    }
}