using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class LoadingScreen : MonoBehaviour
{
    public enum Style
    {
        Fade,
        ShrinkSweep,
        Instant
    }

    [SerializeField] Style FadeInStyle;
    [SerializeField] Style FadeOutStyle;
    [SerializeField] string LoadingTextPrefix;
    [SerializeField] string LoadingTextPostfix;
    [SerializeField] float PrewarmDuration;

    void Awake()
    {
        if (_loadingScreen != null && _loadingScreen != GetComponent<UIDocument>())
        {
            Destroy(gameObject);
            return;
        }

        _screen = this;
        _loadingScreen = GetComponent<UIDocument>();
        _loadingScreen.enabled = true;
        _loadingScreen.rootVisualElement.pickingMode = PickingMode.Position;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        gameObject.SetActive(false);
    }

    static void SetStyle(Style style)
    {
        _root.RemoveFromClassList("Fade");
        _root.RemoveFromClassList("ShrinkSweep");
        _root.RemoveFromClassList("Instant");
        _root.AddToClassList(style.ToString());
        delayTime = style switch
        {
            Style.Fade => 0.3f,
            Style.ShrinkSweep => 0.5f,
            _ => 0
        };
    }

    void OnEnable()
    {
        _bar = _loadingScreen.rootVisualElement.Q(null, "progressbar");
        _root = _loadingScreen.rootVisualElement.Q(null, "loading-root");
        _textDump = _loadingScreen.rootVisualElement.Q("TextDump");
        CurrentState = State.Initializing;
    }

    static UIDocument _loadingScreen;
    static VisualElement _textDump;
    static VisualElement _bar, _root;
    static LoadingScreen _screen;

    public static bool IsLoading => _loadingScreen.gameObject.activeSelf;

    static bool IsShown
    {
        get => _root?.ClassListContains("loading") ?? false;
        set
        {
            if (_root == null)
                return;
            if (value)
                _root.schedule.Execute(() => _root.AddToClassList("loading")).StartingIn(10);
            else
                _root.RemoveFromClassList("loading");
        }
    }

    static bool Show()
    {
        if (_root == null || IsLoading)
            return false;
        _loadingScreen.gameObject.SetActive(true);
        _textDump.Clear();
        IsShown = true;
        SetStyle(_screen.FadeInStyle);
        return true;
    }

    public static State CurrentState { get; private set; }

    public enum State
    {
        Initializing,
        FadeIn,
        Loading,
        FadeOut
    }

    static bool Hide()
    {
        if (TotalLoaders > 0)
            return false;

        IsShown = false;
        SetProgressBar(1);
        delayTime -= Time.deltaTime;
        if (delayTime > 0)
            return false;

        delayTime = 0;
        _loadingScreen.gameObject.SetActive(false);
        Loaders.Clear();
        return true;
    }

    static void SetProgressBar(float progress)
    {
        _bar.style.width = new Length(progress * 100, LengthUnit.Percent);
        _bar.style.display = progress > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    static List<LoadingScreenTask> Loaders = new();
    static float delayTime;

    public static void Prewarm(string text)
    {
        Show();
        LoadingScreenTask.From(Task.Delay((int)(_screen.PrewarmDuration * 1000)), text);
    }

    public static void Load(Task task, string text, bool requiresLoadingScreen = true)
    {
        if (requiresLoadingScreen)
            Show();
        LoadingScreenTask.From(task, text);
    }

    public static void Load(AsyncOperation op, string text, bool requiresLoadingScreen = true)
    {
        if (requiresLoadingScreen)
            Show();
        LoadingScreenTask.From(op, text);
    }

    public static void Load(AsyncOperationHandle op, string text, bool requiresLoadingScreen = true)
    {
        if (requiresLoadingScreen)
            Show();
        LoadingScreenTask.From(op, text);
    }

    static int TotalLoaders => Loaders.Count;

    void Update()
    {
        Loaders = Loaders.Where(x => !x.TryComplete).ToList();

        switch (CurrentState)
        {
            case State.Initializing:
                SetProgressBar(0);
                CurrentState = State.FadeIn;
                break;
            case State.FadeIn:
                delayTime -= Time.deltaTime;
                if (delayTime <= 0)
                {
                    CurrentState = State.Loading;
                    SetStyle(_screen.FadeOutStyle);
                }

                break;
            case State.Loading:
                if (TotalLoaders > 0)
                    SetProgressBar(Loaders[0].Progress);
                else
                    CurrentState = State.FadeOut;

                break;
            case State.FadeOut:
                if (TotalLoaders > 0)
                {
                    delayTime = 0;
                    CurrentState = State.FadeIn;
                    SetStyle(Style.Instant);
                    IsShown = true;
                }
                else
                    Hide();

                break;
        }

        Loaders.ForEach(x => { x.Display(); });
    }

    class LoadingScreenTask
    {
        Task task;
        AsyncOperation op1;
        AsyncOperationHandle op2;

        public float Progress
        {
            get
            {
                if (op1 != null)
                    return math.min(1, op1.progress);
                if (op2.IsValid())
                    return math.min(1, op2.PercentComplete);
                return 0f;
            }
        }

        public bool TryComplete
        {
            get
            {
                var result = task?.IsCompleted ?? Progress >= 1;
                if (result)
                {
                    _screen.StartCoroutine(RemoveLabel());
                }

                return result;
            }
        }

        IEnumerator RemoveLabel()
        {
            _label.RemoveFromClassList("active");
            yield return new WaitForSeconds(0.4f);
            _label.RemoveFromHierarchy();
        }

        Label _label;

        string Text
        {
            set
            {
                _label = new Label($"{_screen.LoadingTextPrefix}{value}{_screen.LoadingTextPostfix}");
                var duplicate = Loaders.FirstOrDefault(x => x._label.text == _label.text);
                if (duplicate != null)
                {
                    duplicate.task = task;
                    duplicate.op1 = op1;
                    duplicate.op2 = op2;
                }
                else
                {
                    Loaders.Add(this);
                    Display();
                }
            }
        }

        public void Display()
        {
            if(_root == null)
                return;
            if (_label.parent == null)
            {
                if (!_textDump.Children().Any(x => (x as Label).text == _label.text))
                    _textDump.Add(_label);
            }
            else
                _label.AddToClassList("active");
        }

        public static LoadingScreenTask From(Task t, string text) => new LoadingScreenTask { task = t, Text = text };

        public static LoadingScreenTask From(AsyncOperation op, string text) =>
            new LoadingScreenTask { op1 = op, Text = text };

        public static LoadingScreenTask From(AsyncOperationHandle handle, string text) =>
            new LoadingScreenTask { op2 = handle, Text = text };
    }
}