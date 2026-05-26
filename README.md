This work was done in DMF Lab and contains two main projects:
* [XR Analysis-Based Design Tools](#xr-analysis-based-design-tools)
* [MR-based CFD Style Simulations](#mr-based-cfd-style-simulations)

# XR Analysis-Based Design Tools

A collection of XR-based engineering design tools developed in Unity for interactive mechanism exploration, assembly creation, and physics-informed design workflows.

Developed at the **DMF Lab, University of Bristol**.

---

## Overview

This project explores how Extended Reality (XR) can support stakeholder-driven engineering design workflows by enabling real-scale interaction, rapid design iteration, motion exploration, and physics-informed system design.

The tools were developed to address a common challenge in engineering review processes: stakeholders often request design modifications during meetings, but conventional CAD workflows make it difficult to implement and evaluate those changes immediately.

This project demonstrates how XR can provide a more interactive and spatial approach to design review by allowing users to:
- Explore articulated mechanisms at full scale
- Modify assemblies interactively
- Create kinematic systems in XR
- Design geometry around physics-driven behaviours
- Prototype pneumatic circuits interactively

---

# Main Scenes

## 1. HospitalRoom_Hinge_based_kinematic

This scene demonstrates a fully articulated hospital light assembly with complete kinematic behaviour simulation.

### Features
- Full linked kinematic motion
- Real-time articulation
- Interactive spotlight manipulation
- Workspace accessibility exploration

### Interaction
Users can:
- Grab and move the spotlight handle
- Explore reachable workspace regions
- Observe linked assembly behaviour in real time
- Understand articulation constraints spatially

This scene focuses on mechanism exploration and workspace analysis.

---

## 2. HospitalRoom_Rot_transformers

This scene extends the hospital light assembly interaction by enabling direct manipulation of individual links and in-situ assembly creation.

### Features
- Link-by-link articulation
- Kinematic motion constraints
- Adjustable link dimensions
- Interactive assembly creation system

### Interaction
Users can:
- Rotate individual links
- Modify link lengths
- Assemble mechanisms in XR
- Create rotational joints using pins

### Assembly Workflow
The assembly space allows users to:
1. Bring links together
2. Insert pins between aligned joints
3. Create rotational constraints dynamically
4. Build articulated assemblies interactively

This scene explores XR-based mechanism creation and intuitive assembly workflows.

---

## 3. PPT_CAD_and_Pneumatics

This scene contains two physics-informed XR design tools.

---

### A. Impact-Driven Systems Design

This module focuses on geometry creation and arrangement based on external stimuli and motion behaviour.

#### Falling Spheres

Users arrange cubes to create pathways such that falling spheres touch all target spheres.

##### Design Goals
- Spatial arrangement optimisation
- Motion-guided geometry placement
- Dynamic interaction reasoning

##### Engineering Relevance
Represents concepts related to:
- Fixture design
- Guidance systems
- Passive interaction systems
- Spatial design under dynamic conditions

---

#### Cam Design

Users design a cam profile that enables a ram to continuously interact with moving target boxes.

##### Features
- Editable cam geometry
- Adjustable cam speed
- Real-time motion feedback

##### Engineering Relevance
Demonstrates:
- Mechanism synthesis
- Motion transfer
- Behaviour-driven geometry design

---

### B. Pneumatic Circuit Design

This module provides an interactive toolkit for designing pneumatic systems in XR.

### Features
- Pneumatic component library
- Circuit layout creation
- Behaviour-driven system interaction
- Real-time component assembly

### Interaction
Users can:
- Create pneumatic circuits
- Connect components interactively
- Explore actuation logic
- Understand system behaviour spatially

This module investigates how XR can support understanding of physics-informed engineering systems beyond static CAD workflows.

---

## Tech Stack

| Component | Technology |
|---|---|
| Engine | Unity |
| Language | C# |
| XR Framework | OpenXR |
| SDK | Meta SDK |
| Hardware | Meta Quest 3 |

---

## Project Structure

```text
Assets/
├── Pro2/
│   ├── HospitalRoom_Hinge_based_kinematic
│   ├── HospitalRoom_Rot_transformers
│   └── PPT_CAD_and_Pneumatics
```

## Running the Project

### Requirements
- Unity
- Meta Quest 3
- Android Build Support
- Meta XR SDK
- OpenXR Plugin

### Setup

1. Clone the repository:

```bash
git clone https://github.com/<your-username>/<repository-name>.git
```

2. Open the project in Unity

3. Open any of the main scenes:
- `HospitalRoom_Hinge_based_kinematic`
- `HospitalRoom_Rot_transformers`
- `PPT_CAD_and_Pneumatics`


# MR-based CFD Style Simulations

Mixed Reality (MR) based low-fidelity CFD simulation platform for interactive drone wing design and airflow visualisation in Unity.

Developed at the **DMF Lab, University of Bristol**.

---

## Overview

D2N Sim Wing is a Unity-based Mixed Reality application that enables real-time interaction with drone wing geometries while visualising airflow behaviour using low-fidelity CFD-style simulations.

The project explores how MR can support faster and more interactive engineering design workflows by allowing users to:

- Modify wing geometry interactively
- Observe airflow changes in real time
- Rapidly explore design iterations
- Visualise aerodynamic behaviour at real scale in immersive space

The system was developed using **Unity 6**, **Meta SDK**, and **OpenXR**, and deployed on the **Meta Quest 3** headset.

---

## Features

### Interactive Geometry Modification
- Control-point based mesh deformation
- Real-time mesh updates
- Smooth weighted vertex displacement

### Real-Time Flow Analysis
- Grid-based low-fidelity fluid simulation
- Semi-Lagrangian advection
- Pressure correction using Gauss–Seidel iteration

### Multiple Flow Visualisation Modes
- Smoke Field
- Pressure Field
- Velocity Field
- Streamlines
- Full 3D Flow Simulation

### Mixed Reality Interaction
Users can:
- Modify wing geometry
- Move the analysis/intersection plane
- Switch between simulation modes in MR

---

## Tech Stack

| Component | Technology |
|---|---|
| Engine | Unity 6 |
| Language | C# |
| XR Framework | OpenXR |
| SDK | Meta SDK |
| Hardware | Meta Quest 3 |

---

## Project Structure

```text
D2N/
└── Assets
    └──Materials
    └──Prefabs
    └──Scripts
    └── D2N Sim Wing
```

All Unity project files and scenes are contained inside the `D2N` folder.

---

## How It Works

The system consists of two primary backend modules:

### 1. Mesh Modification
Wing geometry is modified through interactive control points that deform mesh vertices using distance-weighted displacement for smooth continuous deformation.

### 2. Flow Simulation
The airflow simulation uses:
- Velocity propagation
- Pressure solving
- Smoke advection
- Streamline generation

The solution prioritises responsiveness and interactivity over high-fidelity CFD accuracy, making it suitable for conceptual and early-stage design evaluation.

---

## Running the Project

### Requirements
- Unity 6
- Meta Quest 3
- Android Build Support for Unity
- Meta XR SDK
- OpenXR Plugin

### Setup

1. Clone the repository:

```bash
git clone https://github.com/<your-username>/<repository-name>.git
```

2. Open the project in Unity hub

3. Navigate to and open scene:

```text
D2N/Assets/D2N Sim Wing.unity
```
