#  VR Surgical Simulation                 
High-fidelity surgical training system for Unity with realistic tissue deformation, precision cutting mechanics, and full VR controller integration.              
# Overview                                       
VR Surgical Simulation is a complete Unity framework for creating realistic surgical training experiences. Built from the ground up for virtual reality, it provides soft-body tissue physics, precision cutting tools, and natural hand interactions.
Perfect for:

* Medical training applications
* VR educational experiences
* Surgical procedure simulation
* Healthcare skill development
# Features
**Realistic Tissue Deformation**

* Elastic Mesh System: Realistic soft-body physics for organic tissue
* Material Transitions: Dynamic skin-to-flesh material changes during cutting
* Permanent Deformation: Cut areas retain their deformation state
* Edge Anchoring: Natural tissue behavior with edge stabilization

Show Image  

**Surgical Tools:**

* Scalpel Controller: Full VR controller integration with haptic feedback
* Pick & Place: Natural tool handling with smooth return-to-position  
* Cutting Mechanics: Raycast-based precision cutting system  
* Tool Radius Control: Adjustable influence area for each tool

Show Image    

**VR Integration:**

* Touch-Based Interaction: Direct hand-to-tissue manipulation
* Haptic Feedback: Controller vibration on tissue contact
* Dual Mode System: Switch between pull and cut modes
* Visual Feedback: Real-time ray indicators and color coding

Show Image

**Advanced Systems:**

* Multi-Material Support: Separate materials for skin, flesh, and wounds
* Vertex Region Mapping: Track separated tissue sections
* Cut Influence Zones: Reduced elasticity near incisions
* Debug Visualization: Extensive Gizmos and logging for development

show image 

# Requirements                    
**Software**                     

* Unity 6 or newer                    
* XR Interaction Toolkit                   
* OpenXR Plugin                            

**Hardware**                     

* VR Headset (Quest, Index, Vive, Pico, etc.)                    
* VR-ready PC or standalone Quest                   

# Installation                 
**1. Download the project**   

bash git clone https://github.com/Medin-Egypt/SameSim

**2. Open in Unity Hub**  

Add project folder and wait for import              

**3. Configure XR**           

Enable OpenXR in Project Settings          
Add your controller interaction profiles                 

**4. Test the demo**            

Open `Scenes/SurgicalDemo.unity` and press Play  

# Example Workflow
Basic Setup: Create Your First Surgical Scene
Step 1: Setup XR Environment
Import XR Origin with camera and VR controllers
Show Image
What happens: Unity creates the VR rig with hand tracking

Step 2: Add Tissue Object
Create a 3D model and add ElasticMesh + SurgicalMesh components
Show Image
What happens: Your mesh becomes interactive soft-body tissue

Step 3: Configure Materials
Assign skin material and flesh material in inspector
Show Image
What happens: System prepares dual-material rendering

Step 4: Add Touch Interaction
Attach VRElasticTouchInteraction to controller children
Show Image
What happens: Controllers can now push and deform tissue

Step 5: Setup Scalpel Tool
Import scalpel model and configure ScalpelController
Show Image
What happens: Scalpel becomes pickable with cutting functionality

Step 6: Test and Adjust
Play the scene and fine-tune parameters in real-time
Show Image
What happens: Interactive testing with live parameter adjustment


##  Contributors

<div align="center">

###  Team Members

<table>
<tr>
<td align="center">
<a href="https://github.com/MhmdSheref">
<img src="https://github.com/MhmdSheref.png" width="100px;" alt="MhmdSheref"/><br />
<sub><b>MhmdSheref</b></sub>

</td>

<td align="center">
<a href="https://github.com/BasselM0stafa">
<img src="https://github.com/BasselM0stafa.png" width="100px;" alt="BasselM0stafa"/><br />
<sub><b>Bassel Mostafa</b></sub>
</td>

<td align="center">
<a href="https://github.com/MahmoudZah">
<img src="https://github.com/MahmoudZah.png" width="100px;" alt="MahmoudZah"/><br />
<sub><b>Mahmoud Zahran</b></sub>

</td>

<td align="center">
<a href="https://github.com/RwanOtb">
<img src="https://github.com/RwanOtb.png" width="100px;" alt="RwanOtb"/><br />
<sub><b>RwanOtb</b></sub>
</td>
</tr>
</table>



