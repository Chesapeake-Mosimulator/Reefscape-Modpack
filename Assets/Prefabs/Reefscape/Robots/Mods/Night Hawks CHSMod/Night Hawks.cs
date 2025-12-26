using System;
using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.TestingMod._614
{
    public class NightHawks : ReefscapeRobotBase
    {
        [Header("Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint Arm;
        [SerializeField] private GenericJoint Intake;

        [Header("Testing Variables")]
        [SerializeField] private float rollerTestVelocity;
        [SerializeField] private float hoverTransitionHeightTest;
        [SerializeField] private float hoverTestHeight;

        [Header("Physics Rollers")]
        [SerializeField]
        private GenericRoller[] intakeRollers;
        private float EEv4PunchVelocity = 6000f;
        private float CanalIntakeVelocity; 

        [Header("PIDS")]
        [SerializeField] private PidConstants ArmPid;
        [SerializeField] private PidConstants IntakePid;

        [Header("Coral Setpoints")]
        [SerializeField] private NightHawksSetpoint stow;
        [SerializeField] private NightHawksSetpoint hover;
        [SerializeField] private NightHawksSetpoint intakeGroundCoral;
        [SerializeField] private NightHawksSetpoint groundL1;
        [SerializeField] private NightHawksSetpoint l2;
        [SerializeField] private NightHawksSetpoint l3;

        [Header("Algae Setpoints")]
        [SerializeField] private NightHawksSetpoint intakeGroundAlgae;
        [SerializeField] private NightHawksSetpoint l2Punch;
        [SerializeField] private NightHawksSetpoint l3Punch;

        [Header("Intake Components")]
        [SerializeField] private ReefscapeGamePieceIntake endEffectorCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake endEffectorAlgaeIntake;
        [SerializeField] private ReefscapeGamePieceIntake intakeCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake intakeAlgaeIntake;

        [Header("Game Piece States")]
        [SerializeField] private GamePieceState endEffectorCoralStowState;
        [SerializeField] private GamePieceState endEffectorCoralIntakeState;
        [SerializeField] private GamePieceState algaeStowState;
        [SerializeField] private GamePieceState algaePunch;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private float _elevatorTargetHeight;
        private float _armTargetAngle;
        private float _intakeTargetAngle;

        
        

        private bool StationMode;
        private bool GroundMode;

        protected override void Start()
        {
            base.Start();

            Arm.SetPid(ArmPid);
            Intake.SetPid(IntakePid);

            _elevatorTargetHeight = 0;
            _armTargetAngle = 0;
            _intakeTargetAngle = 0;
            //_hoverTransitionHeight = hoverTransitionHeightTest;
            //_hoverHeight=hoverTestHeight;
        

            RobotGamePieceController.SetPreload(endEffectorCoralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[] { endEffectorCoralStowState, endEffectorCoralIntakeState };
            _coralController.intakes.Add(endEffectorCoralIntake);
            _coralController.intakes.Add(intakeCoralIntake);
            
              _algaeController.gamePieceStates = new[] { algaeStowState };
            //_algaeController.intakes.Add(algaePunch);

            StationMode = true;
        }

        private void LateUpdate()
        {
            Arm.UpdatePid(ArmPid);
            Intake.UpdatePid(IntakePid);
        }


        private void FixedUpdate()
        {

            //NightHawksHoverSequence();


            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();

            _algaeController.SetTargetState(algaeStowState);
            _coralController.SetTargetState(endEffectorCoralStowState);

            bool isPunchingAlgae = IntakeAction.IsPressed() && (CurrentSetpoint == ReefscapeSetpoints.LowAlgae || CurrentSetpoint == ReefscapeSetpoints.HighAlgae);

            if (isPunchingAlgae)
            {
                foreach (var roller in intakeRollers)
                {
                    roller.ChangeAngularVelocity(EEv4PunchVelocity);
                }
            }

            bool isHovering= IntakeAction.IsPressed() && CurrentSetpoint== ReefscapeSetpoints.Intake;
            
            if (isHovering)
            {
                foreach (var canalRoller in intakeRollers) //need a new canal field bc it interferes with the punch roller and causes the arm to wig out
                {
                    canalRoller.ChangeAngularVelocity(rollerTestVelocity);
                }
            }
            else
            {
                foreach (var canalRoller in intakeRollers)
                {
                    canalRoller.ChangeAngularVelocity(0);
                }
            }
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Intake:
                    StartCoroutine(NightHawksHoverSequence());
                    _coralController.RequestIntake(endEffectorCoralIntake, CurrentRobotMode == ReefscapeRobotMode.Coral && !hasCoral);
                    break;
                case ReefscapeSetpoints.Place:
                    PlacePiece();
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(groundL1);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(l2Punch);
                    //_algaeController.RequestIntake(algaePunch, IntakeAction.IsPressed() && !hasAlgae);
                    //_coralController.RequestIntake(endEffectorCoralIntake, false);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(l3Punch);
                    //_algaeController.RequestIntake(algaePunch, IntakeAction.IsPressed() && !hasAlgae);
                    //_coralController.RequestIntake(endEffectorCoralIntake, false);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
            }
            UpdateSetpoints();

        }
        private float _hoverTransitionHeight=22;
        private float _armHoverAngle = 160;
        private float _hoverHeight=16;

        private IEnumerator NightHawksHoverSequence()
        {
            if (IntakeAction.IsPressed() && StationMode == true)
            {
                _elevatorTargetHeight = _hoverTransitionHeight;
                yield return new WaitForSeconds(0.5f);
                _armTargetAngle = _armHoverAngle;
                yield return new WaitForSeconds(1f);
                _elevatorTargetHeight = _hoverHeight;
            }
        }
        private void PlacePiece()
        {
            if (CurrentRobotMode == ReefscapeRobotMode.Algae)
            {
                _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, 0));
            }
            else
                if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 6));
                }

        }

        private void SetSetpoint(NightHawksSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _armTargetAngle = setpoint.armAngle;
            _intakeTargetAngle = setpoint.intakeAngle;
        }
        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            Arm.SetTargetAngle(_armTargetAngle).withAxis(JointAxis.X);
            Intake.SetTargetAngle(_intakeTargetAngle).withAxis(JointAxis.X);
        }
    }
}