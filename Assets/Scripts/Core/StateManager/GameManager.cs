using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameStateManager StateManager { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        StateManager = new GameStateManager();
        StateManager.RegisterStates(new Dictionary<GameStateType, IState>
        {
            { GameStateType.Exploration,    new ExplorationState()    },
            { GameStateType.Dialogue,       new DialogueState()       },
            { GameStateType.Planning,       new PlanningState()       },
            { GameStateType.Puzzle,         new PuzzleState()         },
            { GameStateType.BridgeBuilder,  new BridgeBuilderState()  },
            { GameStateType.RoutineBuilder, new RoutineBuilderState() },
            { GameStateType.KanbanBuilder,  new KanbanBuilderState()  },
            { GameStateType.Reflection,     new ReflectionState()     },
            { GameStateType.MissionBoard,   new MissionBoardState()   },
            { GameStateType.DayComplete,    new DayCompleteState()    },
            { GameStateType.InfoBoard,      new InfoBoardState()      },
            { GameStateType.SettingsMenu,   new SettingsMenuState()   },
        });
    }

    private void Start()
    {
        StateManager.ChangeState(GameStateType.Exploration);
    }

    private void Update()
    {
        StateManager.Update();
    }
}
