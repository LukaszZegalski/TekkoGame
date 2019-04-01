using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;


public class BoardMenager : MonoBehaviour
{
    //wielkość pojedynczej płytki planszy
    private const float TILE_SIZE = 1.0f;
    //przesunięcie, wukorzystywane przy obliczaniu środków 
    private const float TILE_OFFSET = 0.5f;
    //Wyświetlane napisy
    private const string PhaseOne = "Phase I";
    private const string PhaseTwo = "Phase II";
    private const string PhaseOneTwo = "Phase I/II";
    private const string SetBlack = "Round black\nSet pawns";
    private const string SetWhite = "Round white\nSet pawns";
    private const string MoveBlack = "Round black\nmove pawns";
    private const string MoveWhite = "Round white\nmove pawns";
    //zmienne wykorzystywana do obliczenia fazy gry, reprezentujące iość pionów na planszy
    private int amountBlack=0;
    private int amountWhite=0;
    private bool isPhasrOneTwo = false;
    //współrzędne kursora, wartość -1 oznacza że znajduje się poza planszą gry
    private int selectionX = -1;
    private int selectionY = -1;
    //zmienna określająca maksymalny czas jakim dysponuje gracz na wykonanie ruchu
    private static float RoundTime = 300f;
    //Wybrany pion
    private Pawns selectedPawns;
    //lista z aktywnumi pionami (wybrnymi)
    private List<GameObject> PawnActive = new List<GameObject>();
    //zmienna przechowująca początkowy czas rozpocęcia zagrania przez danego gracza
    private float StartTime;
    //przechowuje możliwe ruchy danego piona w celu ich wuświetlenia
    private bool[,] allowedMoves { get; set; }

    //lista z pionami w grze
    public List<GameObject> PawnsPrefabs;
    //Określa jaki gracz aktualnie wykonuje swoją turę
    public bool isWhiteTure = true;
    //zmienna przechowująca nazwę gracza aktualnie wykonującego ruch
    public Text PlayerMove;
    //zmienna określająca nazwę rundy (wykładanie, ruchy)
    public Text NameRound;
    //zmienna przechowuwująca czas do końca rundy 
    public Text TimeRound;
    //zmianna odwołująca się do obiektu audioMixer
    public AudioMixer audioMixer;
    //zmienna odwołująca się do obiektu Slider w grze 
    public Slider audioSlider;
    //zmienna bubliczna klasy BoardMenager
    public static BoardMenager Instance { get; set; }
    //tablica reprezentująca plansze 
    public Pawns[,] Pawns { get; set; }

    //Wykonywane przy starsie gry
    private void Start()
    {
        //ustawienie Slidera do wartości aktualnego poziomu głośności w grze
        UpdateSlider();
        //deklaracja zmiennych
        CreateDeclareObject();
    }

    //Wykonywane na okrągło podczas rozgrywki (główna pętla gry)
    private void Update()
    {
        //aktualizacja czasu gry
        UpdateTime();
        //sprawdzenie czy koniec rundy
        CheckTimeRound();
        //Sprawdanie czy występuje faza I/II
        CheckPhaseOneTwo();
        //ustawienie nazwy fazy
        SetPhase();
        //ustawienie rodzaju ruchu dla aktualnego gracza
        SetPlayerMove();
        //aktualizacja położenia kursora
        UpdateSelection();
        //Wybór wykonanie ruchu|postawienie piona
        MoveOrSetPawns();
        //Sprawdzenie czy wystąpiła konfiguracja wygrywająca
        CheckWin();
    }

    //Deklaracja zmiennych 
    private void CreateDeclareObject()
    {
        StartTime = Time.time;
        PawnActive = new List<GameObject>();
        Pawns = new Pawns[5, 5];
        PlayerMove.text = "";
        NameRound.text = "";
        Instance = this;
    }

    //Wybranie danego piona
    private void SelectPawns(int x, int y)
    {
        if (Pawns[x, y] == null)
            return;
        if (Pawns[x, y].isWhite != isWhiteTure)
            return;
        allowedMoves = Pawns[x, y].PosibleMove();
        selectedPawns = Pawns[x, y];
        BoardSquareLights.Instance.SquareLightAllowedMoves(allowedMoves);
    }

    //Przemieszczenie piona
    private void MovePawns(int x, int y)
    {
        if (allowedMoves[x, y])
        {
            Pawns[selectedPawns.CurentX, selectedPawns.CurentY] = null;
            selectedPawns.transform.position = GetTitleCenter(x, y);
            selectedPawns.SetPosition(x, y);
            Pawns[x, y] = selectedPawns;
            isWhiteTure = !isWhiteTure;
            StartTime = Time.time;
        }
        BoardSquareLights.Instance.HidenSquareLights();
        selectedPawns = null;
    }

    //Załadowanie pojedynczego piona wykorzystywane w pierwszej fazie gry
    private void SpawnPawns(int index, int x, int y)
    {
        GameObject go = Instantiate(PawnsPrefabs[index], GetTitleCenter(x, y), Quaternion.identity) as GameObject;
        go.transform.SetParent(transform);
        Pawns[x, y] = go.GetComponent<Pawns>();
        Pawns[x, y].SetPosition(x, y);
        PawnActive.Add(go);
        StartTime = Time.time;
    }

    //Funkcja stawiająca pion na wybrane pole bądź przemieszczająca już istniejące 
    private void MoveOrSetPawns()
    {
        if (isWhiteTure)
        {
            if (amountWhite != 4)
                SetPawnsOnSelectSquare();
            else
                MovePawnsOnSelectSquare();
        }
        else
        {
            if (amountBlack != 4)
                SetPawnsOnSelectSquare();
            else
                MovePawnsOnSelectSquare();
        }
    }

    //Funkcja stawiająca piona na wybranym polu
    private void MovePawnsOnSelectSquare()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (selectionX >= 0 && selectionY >= 0)
            {
                if (selectedPawns == null)
                {
                    SelectPawns(selectionX, selectionY);
                }
                else
                {
                    MovePawns(selectionX, selectionY);
                }
            }
        }
    }
    
    //Funkcja przemieszczająca wybrany pion
    private void SetPawnsOnSelectSquare()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (selectionX >= 0 && selectionY >= 0)
            {
                if (Pawns[selectionX, selectionY] == null)
                {
                    if (isWhiteTure)
                    {
                        SpawnPawns(1, selectionX, selectionY);
                        amountWhite += 1;
                    }
                    else
                    {
                        SpawnPawns(0, selectionX, selectionY);
                        amountBlack += 1;
                    }
                    isWhiteTure = !isWhiteTure;
                }
            }
        }
    }

    //Zwraca współrzędne środków pól z wektora reprezentującego planszę 
    private Vector3 GetTitleCenter(int x, int y)
    {
        Vector3 origin = Vector3.zero;
        origin.x += (TILE_SIZE * x) + TILE_OFFSET;
        origin.z += (TILE_SIZE * y) + TILE_OFFSET;
        return origin;
    }

    //Aktualizowanie wybranego pola po zmianie położenia kursora
    private void UpdateSelection()
    {
        if (!Camera.main)
            return;
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 25.0f, LayerMask.GetMask("TeekoPlane")))
        {
            selectionX = (int)hit.point.x;
            selectionY = (int)hit.point.z;
        }
        else
        {
            selectionX = -1;
            selectionY = -1;
        }
    }

    //Sprawdzanie czy po danym ruchu nastąpił wygrana
    private void CheckWin()
    {
        if (CheckDiagonal() || CheckHorizontal() || CheckSquare() || CheckVertical())
            EndGame();
    }

    //Sprawdzenie czy piony ułożone są w lini poziomej
    private bool CheckHorizontal()
    {
        for (int i = 0; i < 5; i++)
        {
            if ((Pawns[0, i] != null && Pawns[1, i] != null && Pawns[2, i] != null && Pawns[3, i] != null))
                if ((Pawns[0, i].isWhite && Pawns[1, i].isWhite && Pawns[2, i].isWhite && Pawns[3, i].isWhite))
                    return true;

            if ((Pawns[1, i] != null && Pawns[2, i] != null && Pawns[3, i] != null && Pawns[4, i] != null))
                if ((Pawns[1, i].isWhite && Pawns[2, i].isWhite && Pawns[3, i].isWhite && Pawns[4, i].isWhite))
                    return true;

            if ((Pawns[0, i] != null && Pawns[1, i] != null && Pawns[2, i] != null && Pawns[3, i] != null))
                if (!(Pawns[0, i].isWhite || Pawns[1, i].isWhite || Pawns[2, i].isWhite || Pawns[3, i].isWhite))
                    return true;

            if ((Pawns[1, i] != null && Pawns[2, i] != null && Pawns[3, i] != null && Pawns[4, i] != null))
                if (!(Pawns[1, i].isWhite || Pawns[2, i].isWhite || Pawns[3, i].isWhite || Pawns[4, i].isWhite))
                    return true;
        }
        return false;
    }

    //Sprawdzenie czy piony ułożone są w lini pionowej
    private bool CheckVertical()
    {
        for (int i = 0; i < 5; i++)
        {
            if ((Pawns[i, 0] != null && Pawns[i, 1] != null && Pawns[i, 2] != null && Pawns[i, 3] != null))
                if ((Pawns[i, 0].isWhite && Pawns[i, 1].isWhite && Pawns[i, 2].isWhite && Pawns[i, 3].isWhite))
                    return true;
            if ((Pawns[i, 1] != null && Pawns[i, 2] != null && Pawns[i, 3] != null && Pawns[i, 4] != null))
                if ((Pawns[i, 1].isWhite && Pawns[i, 2].isWhite && Pawns[i, 3].isWhite && Pawns[i, 4].isWhite))
                    return true;
            if ((Pawns[i, 0] != null && Pawns[i, 1] != null && Pawns[i, 2] != null && Pawns[i, 3] != null))
                if (!(Pawns[i, 0].isWhite || Pawns[i, 1].isWhite || Pawns[i, 2].isWhite || Pawns[i, 3].isWhite))
                    return true;
            if ((Pawns[i, 1] != null && Pawns[i, 2] != null && Pawns[i, 3] != null && Pawns[i, 4] != null))
                if (!(Pawns[i, 1].isWhite || Pawns[i, 2].isWhite || Pawns[i, 3].isWhite || Pawns[i, 4].isWhite))
                    return true;
        }
        return false;
    }

    //Sprawdzenie czy piony ułożone są w lini skośnej
    private bool CheckDiagonal()
    {
        if ((Pawns[0, 1] != null && Pawns[1, 2] != null && Pawns[2, 3] != null && Pawns[3, 4] != null))
            if ((Pawns[0, 1].isWhite && Pawns[1, 2].isWhite && Pawns[2, 3].isWhite && Pawns[3, 4].isWhite))
                return true;

        if ((Pawns[0, 0] != null && Pawns[1, 1] != null && Pawns[2, 2] != null && Pawns[3, 3] != null))
            if ((Pawns[0, 0].isWhite && Pawns[1, 1].isWhite && Pawns[2, 2].isWhite && Pawns[3, 3].isWhite))
                return true;

        if ((Pawns[1, 1] != null && Pawns[2, 2] != null && Pawns[3, 3] != null && Pawns[4, 4] != null))
            if ((Pawns[1, 1].isWhite && Pawns[2, 2].isWhite && Pawns[3, 3].isWhite && Pawns[4, 4].isWhite))
                return true;

        if ((Pawns[1, 0] != null && Pawns[2, 1] != null && Pawns[3, 2] != null && Pawns[4, 3] != null))
            if ((Pawns[1, 0].isWhite && Pawns[2, 1].isWhite && Pawns[3, 2].isWhite && Pawns[4, 3].isWhite))
                return true;

        if ((Pawns[0, 4] != null && Pawns[1, 3] != null && Pawns[2, 2] != null && Pawns[3, 1] != null))
            if ((Pawns[0, 4].isWhite && Pawns[1, 3].isWhite && Pawns[2, 2].isWhite && Pawns[3, 1].isWhite))
                return true;

        if ((Pawns[0, 3] != null && Pawns[1, 2] != null && Pawns[2, 1] != null && Pawns[3, 0] != null))
            if ((Pawns[0, 3].isWhite && Pawns[1, 2].isWhite && Pawns[2, 1].isWhite && Pawns[3, 0].isWhite))
                return true;

        if ((Pawns[1, 4] != null && Pawns[2, 3] != null && Pawns[3, 2] != null && Pawns[4, 1] != null))
            if ((Pawns[1, 4].isWhite && Pawns[2, 3].isWhite && Pawns[3, 2].isWhite && Pawns[4, 1].isWhite))
                return true;

        if ((Pawns[4, 0] != null && Pawns[3, 1] != null && Pawns[2, 2] != null && Pawns[1, 3] != null))
            if ((Pawns[4, 0].isWhite && Pawns[3, 1].isWhite && Pawns[2, 2].isWhite && Pawns[1, 3].isWhite))
                return true;


        if ((Pawns[0, 1] != null && Pawns[1, 2] != null && Pawns[2, 3] != null && Pawns[3, 4] != null))
            if (!(Pawns[0, 1].isWhite || Pawns[1, 2].isWhite || Pawns[2, 3].isWhite || Pawns[3, 4].isWhite))
                return true;

        if ((Pawns[0, 0] != null && Pawns[1, 1] != null && Pawns[2, 2] != null && Pawns[3, 3] != null))
            if (!(Pawns[0, 0].isWhite || Pawns[1, 1].isWhite || Pawns[2, 2].isWhite || Pawns[3, 3].isWhite))
                return true;

        if ((Pawns[1, 1] != null && Pawns[2, 2] != null && Pawns[3, 3] != null && Pawns[4, 4] != null))
            if (!(Pawns[1, 1].isWhite || Pawns[2, 2].isWhite || Pawns[3, 3].isWhite || Pawns[4, 4].isWhite))
                return true;

        if ((Pawns[1, 0] != null && Pawns[2, 1] != null && Pawns[3, 2] != null && Pawns[4, 3] != null))
            if (!(Pawns[1, 0].isWhite || Pawns[2, 1].isWhite || Pawns[3, 2].isWhite || Pawns[4, 3].isWhite))
                return true;

        if ((Pawns[0, 4] != null && Pawns[1, 3] != null && Pawns[2, 2] != null && Pawns[3, 1] != null))
            if (!(Pawns[0, 4].isWhite || Pawns[1, 3].isWhite || Pawns[2, 2].isWhite || Pawns[3, 1].isWhite))
                return true;

        if ((Pawns[0, 3] != null && Pawns[1, 2] != null && Pawns[2, 1] != null && Pawns[3, 0] != null))
            if (!(Pawns[0, 3].isWhite || Pawns[1, 2].isWhite || Pawns[2, 1].isWhite || Pawns[3, 0].isWhite))
                return true;

        if ((Pawns[1, 4] != null && Pawns[2, 3] != null && Pawns[3, 2] != null && Pawns[4, 1] != null))
            if (!(Pawns[1, 4].isWhite || Pawns[2, 3].isWhite || Pawns[3, 2].isWhite || Pawns[4, 1].isWhite))
                return true;

        if ((Pawns[4, 0] != null && Pawns[3, 1] != null && Pawns[2, 2] != null && Pawns[1, 3] != null))
            if (!(Pawns[4, 0].isWhite || Pawns[3, 1].isWhite || Pawns[2, 2].isWhite || Pawns[1, 3].isWhite))
                return true;

        return false;
    }

    //Sprawdzenie czy piony ułożone są w kwadracie
    private bool CheckSquare()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (Pawns[i, j] != null && Pawns[i, j + 1] != null && Pawns[i + 1, j] != null && Pawns[i + 1, j + 1] != null)
                    if (Pawns[i, j].isWhite && Pawns[i, j + 1].isWhite && Pawns[i + 1, j].isWhite && Pawns[i + 1, j + 1].isWhite)
                        return true;
                if ((Pawns[i, j] != null && Pawns[i, j + 1] != null && Pawns[i + 1, j] != null && Pawns[i + 1, j + 1] != null))
                    if (!(Pawns[i, j].isWhite || Pawns[i, j + 1].isWhite || Pawns[i + 1, j].isWhite || Pawns[i + 1, j + 1].isWhite))
                        return true;
            }
        }
        return false;
    }

    //Funkcja wywoływana w momencie zkończenia gry poprzez wygranie jednej ze stron
    private void EndGame()
    {
        foreach (GameObject go in PawnActive)
            Destroy(go);
        if (isWhiteTure)
            SceneManager.LoadScene(3);
        else
            SceneManager.LoadScene(2);
    }

    //Funkcja wyłowywana w momencie naciśnięcia EXIT, wyjście do menu głównego 
    public void ExitGame()
    {
        foreach (GameObject go in PawnActive)
            Destroy(go);
        SceneManager.LoadScene(0);
    }

    //Funkcja odpowiadająca za restart gry
    public void Resume()
    {
        foreach (GameObject go in PawnActive)
            Destroy(go);
        SceneManager.LoadScene(1);
    }

    //Funkcja ustawiająca aktualny etap gry
    private void SetPhase()
    {
        if (isPhasrOneTwo)
        {
            NameRound.text = PhaseOneTwo;
            return;
        }
        if (amountWhite == 4 && amountBlack == 4)
            NameRound.text = PhaseTwo;
        else
            NameRound.text = PhaseOne;
    }

    private void CheckPhaseOneTwo()
    {
        if (Mathf.Abs(amountBlack - amountWhite) > 1)
            isPhasrOneTwo = true;
        if (amountWhite == 4 && amountBlack == 4)
            isPhasrOneTwo = false;
    }

    //Funkcja ustawiająca informację o ruchu aktualnego gracza
    private void SetPlayerMove()
    {
        if (isWhiteTure)
        {
            if (amountWhite != 4)
                PlayerMove.text = SetWhite;
            else
                PlayerMove.text = MoveWhite;
        }
        else
        {
            if (amountBlack != 4)
                PlayerMove.text = SetBlack;
            else
                PlayerMove.text = MoveBlack;
        }
    }

    //Funkcja ustawiająca położenie Slider'a w momencie załadowania sceny 'Game'
    private void UpdateSlider()
    {
        float volume;
        audioMixer.GetFloat("volume", out volume);
        audioSlider.value = volume;
    }

    //Funkca obliczająca czas jaki upłynął od momentu rozpoczęcia danej tury gracza 
    private void UpdateTime()
    {
        float t = RoundTime - (Time.time - StartTime);
        string minuts = ((int)t / 60).ToString();
        string seconds = "";
        if (((int)(t % 60)) < 10)
            seconds = "0" + ((int)(t % 60)).ToString();
        else
            seconds = ((int)(t % 60)).ToString();

        TimeRound.text = minuts + ":" + seconds;
    }

    //Funkcja sprawdzająca czy dana rozgrywka trwwa dłużej niż 5minut
    private void CheckTimeRound()
    {
        if (TimeRound.text == "0:00")
        {
            StartTime = Time.time;
            isWhiteTure = !isWhiteTure;
        }
    }

}

//FUNKCJE WYKORZYSTYWANE W FAZIE PROJEKTOWANIA MOGONCE ZOSTAĆ WYKORZYSTANE W PUŹNIEJSZYM ETAPIE 
/*
     //Zaznaczenie wybranego pola po najechaniu kurorem
    private void DrawSelection()
    {
        if (selectionX >= 0 && selectionY >= 0)
        {
            Debug.DrawLine(
                Vector3.forward * selectionY + Vector3.right * selectionX,
                Vector3.forward * (selectionY + 1) + Vector3.right * (selectionX + 1), Color.red);
            Debug.DrawLine(
               Vector3.forward * (selectionY + 1) + Vector3.right * selectionX,
               Vector3.forward * selectionY + Vector3.right * (selectionX + 1), Color.red);
        }
    }

    //Rysowanie planszy
    private void DrawTeekoBoard()
    {
        Vector3 widthLine = Vector3.right * 5;
        Vector3 heightLine = Vector3.forward * 5;

        for (int i = 0; i <= 5; i++)
        {
            Vector3 start = Vector3.forward * i;
            Debug.DrawLine(start, start + widthLine, Color.red);
            for (int j = 0; j <= 5; j++)
            {
                start = Vector3.right * j;
                Debug.DrawLine(start, start + heightLine, Color.red);
            }
        }
    }                        
*/
