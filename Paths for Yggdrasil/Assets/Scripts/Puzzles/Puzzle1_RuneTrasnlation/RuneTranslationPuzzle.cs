using UnityEngine;
using Photon.Pun;
using TMPro;

namespace Yggdrasil.Puzzles.RuneTranslation
{
    public class RuneTranslationPuzzle : BasePuzzle
    {
        [Header("Rune Translation Settings")]
        [SerializeField] private RuneData runeData;
        [SerializeField] private string correctWord = "YGGDRASIL";
        [SerializeField] private Transform runeDisplayParent;
        [SerializeField] private GameObject runeSlotPrefab;

        [Header("Player Assignments")]
        [SerializeField] private bool isPlayerARoom = false; // Player A vê alfabeto, Player B traduz

        private string currentInput = "";

        public override void Initialize()
        {
            base.Initialize();
            SetupRuneDisplay();
        }

        private void SetupRuneDisplay()
        {
            if (isPlayerARoom)
            {
                // Player A: Mostra alfabeto completo
                DisplayFullAlphabet();
            }
            else
            {
                // Player B: Mostra runas para traduzir
                DisplayRunesToTranslate();
            }
        }

        private void DisplayFullAlphabet()
        {
            foreach (var runePair in runeData.alphabet)
            {
                GameObject slot = Instantiate(runeSlotPrefab, runeDisplayParent);
                // Configurar UI para mostrar runa + letra
                // slot.GetComponent<Image>().sprite = runePair.runeSprite;
                // slot.GetComponentInChildren<TMP_Text>().text = runePair.latinChar;
            }
        }

        private void DisplayRunesToTranslate()
        {
            foreach (char c in correctWord)
            {
                GameObject slot = Instantiate(runeSlotPrefab, runeDisplayParent);
                Sprite runeSprite = runeData.GetRuneSprite(c.ToString());
                // Configurar UI para mostrar apenas a runa
                // slot.GetComponent<Image>().sprite = runeSprite;
            }
        }

        public void SubmitTranslation(string translation)
        {
            if (state != PuzzleState.Active) return;

            currentInput = translation.ToUpper();

            if (currentInput == correctWord)
            {
                CompletePuzzle();
            }
            else
            {
                Debug.Log("Tradução incorreta!");
            }
        }

        public override void StartPuzzle()
        {
            base.StartPuzzle();
            currentInput = "";
        }
    }
}