﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class AgentAction
{
    public List<KeyCode> KeyBindings;

    public bool InputActive;

    public bool InputChanged;
    
    public AgentAction(List<KeyCode> keyBindings)
    {
        KeyBindings = keyBindings;
        InputActive = false;
        InputChanged = false;
    }

    public override string ToString()
    {
        return "Agent Action.";
    }

    public void Update()
    {
        if (!InputActive) // don't look for new down if already down
        {
            foreach (var keyCode in KeyBindings)
            {
                if (Input.GetKey(keyCode))
                {
                    // Debug.Log(this.ToString() +" Key Down.");
                    InputActive = true;
                    InputChanged = true;
                    break;
                }
            }
        }
        else
        {
            int keybindUp = KeyBindings.Count;
            foreach (var keyCode in KeyBindings)
            {
                if (Input.GetKey(keyCode))
                {
                    keybindUp--;
                }
            }

            if (keybindUp == KeyBindings.Count && !InputChanged)
            {
                // Debug.Log(this.ToString() +" Key Up.");
                InputActive = false;
                InputChanged = true;
            }
            else
            {
                InputChanged = false;
            }
        }
    }
}

public class MovementAction : AgentAction
{
    public enum Movement {Forward, TurnLeft, TurnRight}

    public Movement action;
    
    public MovementAction(List<KeyCode> keyBindings, Movement movementAction) : base(keyBindings)
    {
        action = movementAction;
    }
    
    public override string ToString()
    {
        switch (action)
        {
            case Movement.TurnLeft:
                return "Movement Action: Turn Left.";
            case Movement.TurnRight:
                return "Movement Action: Turn Right.";
            default:
                return "Unknown Movement Action.";
        }
    }
}

public class CombatAction : AgentAction
{
    public enum Combat {Weapon0,Weapon1}

    public Combat action;
    public CombatAction(List<KeyCode> keyBindings, Combat combatAction) : base(keyBindings)
    {
        action = combatAction;
    }
}
public class InputController
{
    public List<AgentAction> Actions = new List<AgentAction>();

    public List<MovementAction.Movement> MovementQueue = new List<MovementAction.Movement>();

    public InputController()
    {
        Actions.Add(new MovementAction(new List<KeyCode>(new KeyCode[] {KeyCode.A, KeyCode.LeftArrow}), MovementAction.Movement.TurnLeft));
        Actions.Add(new MovementAction(new List<KeyCode>(new KeyCode[] {KeyCode.D, KeyCode.RightArrow}), MovementAction.Movement.TurnRight));
    }
        
    public void Update()
    {
        foreach (var agentAction in Actions)
        {
            agentAction.Update();
                
            // process movement actions
            if (agentAction is MovementAction agentMovementAction)
            {
                if (agentMovementAction.InputActive && agentMovementAction.InputChanged)
                {
                    MovementQueue.Add(agentMovementAction.action);
                    Debug.Log("Added " + agentMovementAction.ToString() +" to the movement queue.");
                }
            }
                
            // process combat actions
            if(agentAction is CombatAction agentCombatAction)
            {
                    
            }
        }
    }

    public MovementAction.Movement ProccessMovementQueue()
    {
        int movementDirection = 0;

        foreach (var movement in MovementQueue)
        {
            if (movement == MovementAction.Movement.TurnLeft)
                movementDirection--;
            if (movement == MovementAction.Movement.TurnRight)
                movementDirection++;
        }
        
        MovementQueue.Clear();

        if (movementDirection < 0) // turn left
        {
            return MovementAction.Movement.TurnLeft;
        }

        if (movementDirection > 0) // turn right
        {
            return MovementAction.Movement.TurnRight;
        }
        
        return MovementAction.Movement.Forward;
    }
}

public class AgentController : MonoBehaviour
{
    private Rigidbody Rigidbody;
    private BoxCollider BoxCollider;

    private Vector3 MovementDirection = Vector3.zero;
    
    private Vector3 NextMovementDirection = Vector3.zero;

    public float Speed = 1.0f;
    public float Tilt = 1.0f;

    private GridController GridController;
    
    private InputController _inputController;
    
    private GridCell.GridPosition _startCell = new GridCell.GridPosition(0,0);

    public GridCell.GridPosition GetStartingCell()
    {
        return _startCell;
    }
    
    public void SetStartingCell(int x, int y)
    {
        if (GridController == null)
        {
            GridController = GameObject.FindObjectOfType<GridController>();
        }
        transform.position = GridController.GetCell(x,y).centre;

        SetCurrentCell(x, y);
        _currentCell.SetSpawn(true);
    }

    private GridCell _currentCell;

    public GridCell GetCurrentCell()
    {
        return _currentCell;
    }

    public void SetCurrentCell(int x, int y)
    {
        _currentCell = GridController.GetCell(x, y);
        _currentCell.agentsInCell.Add(this);
        
        if(MovementDirection == Vector3.left)
            SetNextCell(x-1, y);
        if(MovementDirection == Vector3.forward)
            SetNextCell(x, y+1);
        if(MovementDirection == Vector3.right)
            SetNextCell(x+1, y);
        if(MovementDirection == Vector3.down)
            SetNextCell(x, y-1);
    }
    
    private GridCell _nextCell;

    public GridCell GetNextCell()
    {
        return _nextCell;
    }

    public void SetNextCell(int x, int y)
    {
        _nextCell = GridController.GetCell(x, y);
        _nextCell.ToggleNextCellIndicator();
    }
    
    public void SetNextCell(GridCell nextCell)
    {
        _nextCell = nextCell;
        _nextCell.ToggleNextCellIndicator();
    }
    
    private GridCell _previousCell;

    public GridCell GetPreviousCell()
    {
        return _previousCell;
    }

    public void SetPreviousCell(int x, int y)
    {
        _previousCell = GridController.GetCell(x, y);
    }
    
    public void SetPreviousCell(GridCell previousCell)
    {
        _previousCell = previousCell;
    }

    // Start is called before the first frame update
    private void Start()
    {
        _inputController = new InputController();
        
        Rigidbody = gameObject.GetComponent(typeof(Rigidbody)) as Rigidbody;
        BoxCollider = gameObject.GetComponent(typeof(BoxCollider)) as BoxCollider;
        
        SetMovementDirection(Vector3.forward);
        
        GridController = GameObject.FindObjectOfType<GridController>();
    }

    public void ChangeMovementDirection(MovementAction.Movement newMovement)
    {
        if (MovementDirection == Vector3.forward)
        {
            if(newMovement == MovementAction.Movement.TurnLeft)
                SetMovementDirection(Vector3.left);
            if(newMovement == MovementAction.Movement.TurnRight)
                SetMovementDirection(Vector3.right);
            return;
        }
        
        if (MovementDirection == Vector3.back)
        {
            if(newMovement == MovementAction.Movement.TurnLeft)
                SetMovementDirection(Vector3.right);
            if(newMovement == MovementAction.Movement.TurnRight)
                SetMovementDirection(Vector3.left);
            return;
        }
        
        if (MovementDirection == Vector3.left)
        {
            if(newMovement == MovementAction.Movement.TurnLeft)
                SetMovementDirection(Vector3.back);
            if(newMovement == MovementAction.Movement.TurnRight)
                SetMovementDirection(Vector3.forward);
            return;
        }
        
        if (MovementDirection == Vector3.right)
        {
            if(newMovement == MovementAction.Movement.TurnLeft)
                SetMovementDirection(Vector3.forward);
            if(newMovement == MovementAction.Movement.TurnRight)
                SetMovementDirection(Vector3.back);
            return;
        }
    }
    public void SetMovementDirection(Vector3 movement)
    {
        if(MovementDirection != movement)
            MovementDirection = movement;
    }
    
    //Detect collisions between the GameObjects with Colliders attached
    void OnTriggerEnter(Collider other)
    {
        // Debug.Log("Player entered: " + other.gameObject.name);
        
        // leave previous current and next
        if (_currentCell != null && _currentCell.agentsInCell.Count > 0)
        {
            _currentCell.agentsInCell.Remove(this);
            if (_currentCell.IsSpawn())
            {
                _currentCell.SetSpawn(false);
            }
            SetPreviousCell(_currentCell);
        }

        if (_nextCell != null && _nextCell.IsNextCell())
        {
            _nextCell.ToggleNextCellIndicator();
            _nextCell.agentsInCell.Remove(this);
        }

        SetNextCell(other.gameObject.GetComponent<GridCell>());
        // enter new cells
    }

    void OnTriggerExit(Collider other)
    {
        // Debug.Log("Player left: " + other.gameObject.name);

        GridCell triggerCell = other.gameObject.GetComponent<GridCell>();

        if (triggerCell == _currentCell && !triggerCell.IsSpawn())
        {
            throw new InvalidOperationException("Player leaving grid.");
        }
    }
    void FixedUpdate ()
    {
        _inputController.Update();

        MovementAction.Movement movement = _inputController.ProccessMovementQueue();

        if (movement != MovementAction.Movement.Forward)
            ChangeMovementDirection(movement);
        
        GetComponent<Rigidbody>().velocity = MovementDirection * Speed;

        GetComponent<Rigidbody>().position = new Vector3 
        (
            Mathf.Clamp (GetComponent<Rigidbody>().position.x, BoxCollider.bounds.min.x, BoxCollider.bounds.max.x), 
            0.0f, 
            Mathf.Clamp (GetComponent<Rigidbody>().position.z, BoxCollider.bounds.min.z, BoxCollider.bounds.max.z)
        );

        //GetComponent<Rigidbody>().rotation = Quaternion.Euler (0.0f, 0.0f, GetComponent<Rigidbody>().velocity.x * -Tilt);
        
        if(MovementDirection == Vector3.right)
            transform.rotation = Quaternion.Euler(0,90,0);
        if(MovementDirection == Vector3.left)
            transform.rotation = Quaternion.Euler(0,-90,0);
        if(MovementDirection == Vector3.forward)
            transform.rotation = Quaternion.Euler(0,0,0);
        if(MovementDirection == Vector3.back)
            transform.rotation = Quaternion.Euler(0,180,0);
    }
}
