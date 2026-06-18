# AI Bully for the 2026 GameAI Summer School Jam

[![Unity](https://img.shields.io/badge/Unity-6.0.4-blue.svg)](https://unity.com) [![ML‑Agents](https://img.shields.io/badge/Unity_ML_Agents-4.0.3-green.svg)](https://github.com/Unity-Technologies/ml-agents) [![Python](https://img.shields.io/badge/Python-3.10.11-yellow.svg)](https://python.org) [![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)


## Team Name

**AI BULLIES**

*We bully obnoxious AI*


# ML‑Agents Advanced Installation Guide

This single Markdown document contains everything you need to get Unity’s **ML‑Agents 4.x** running in a reproducible Conda environment with the latest stable GitHub release and a fix for the `pkg_resources` error that appears on modern Python versions.

---

## Overview

- Create an isolated Conda environment (`mlagents`) with Python 3.10.11  
- Install the correct PyTorch wheel (CUDA 12.1)  
- Clone the `release_23` branch of the official ML‑Agents repository  
- Install the two pip packages in the proper order  
- Pin `setuptools==81.0.0` to avoid a `ModuleNotFoundError: No module named 'pkg_resources'` on Python ≥ 3.12  
- Verify with `mlagents-learn --help`

---

## Prerequisites

| Item | Minimum version |
|------|-----------------|
| **Python** | 3.10.11 (or newer) |
| **Conda** | Miniconda / Anaconda |
| **CUDA Toolkit** | CUDA 12.1 (for the GPU‑enabled PyTorch wheel) |
| **Git** | Any recent version |

> ⚙️ If you already have a suitable Python environment, skip to **Step 2: Clone and install ML‑Agents**.

---

## 1️⃣ Create and Activate the Conda Environment

```bash
# following instructions in: https://docs.unity3d.com/Packages/com.unity.ml-agents@4.0/manual/Installation.html
conda create -n mlagents python=3.10.11
conda activate mlagents
pip3 install torch~=2.2.1 --index-url https://download.pytorch.org/whl/cu121

# advanced installation relies on latest stable release from git. We are downloading from this fork since this is the repo for the jam
git clone --branch release_23 https://github.com/carlotes247/2026_AI_Bullying.git

# install packages inside cloned project in the correct order!
cd /path/to/ml-agents
python -m pip install ./ml-agents-envs
python -m pip install ./ml-agents

# when calling mlagents-learn --help I get the error ModuleNotFoundError: No module named 'pkg_resources'
# apparently pkg_resources has been removed from "modern" python versions so we need to install an older version
# see the end of https://github.com/ManimCommunity/manim/issues/3585
pip install setuptools==81.0.0

# this should work now with a warning about pkg_resources only
mlagents-learn --help 
```
## Install Unity 6.0.40

Below are the step‑by‑step instructions to install the **Unity 6.0.40** editor on Windows, macOS, and Linux.  
The process relies on **Unity Hub**, which manages multiple Unity installations, modules, and licenses in a single place.

> ⚡ *Tip:* If you already have an older Unity version installed via Hub, simply add 6.0.40 to the same instance instead of creating a fresh one.

---

### 1️⃣ Prerequisites

| Item | Minimum requirement |
|------|---------------------|
| **Operating System** | Windows 10/11, macOS 13 (Ventura) or later, Ubuntu 22.04 LTS or newer |
| **Disk Space** | ≥ 15 GB free (the editor itself + modules) |
| **Network** | Reliable internet connection for downloading ~ 3–5 GB of data |
| **Optional** | Unity ID / license if you’re not using the free tier |

---

### 2️⃣ Download & Install Unity Hub

1. Go to the official download page:  
   <https://unity.com/download>
2. Click *Download Unity Hub* for your OS.
3. Run the installer and follow the on‑screen prompts.

> **Linux note:** On Ubuntu, you can also install via the `snap` store:  
> ```bash
> sudo snap install unityhub --classic
> ```

---

### 3️⃣ Add Unity 6.0.40 through Hub

#### Windows / macOS

1. Open **Unity Hub**.
2. Go to the **Installs** tab and click the green **Add** button.
3. In the “Choose a version” dialog, select **Download Archive** → **2024.2.0f1** (the tag for 6.0.x) or use the search box to find **Unity 6.0.40** directly.
4. Click **Next**.  
   Unity Hub will download a bootstrapper (~30 MB).  
5. The installer will ask you to select modules:  
   - `Unity 6.0.40 (Full)` – core engine and editor.  
   - Optional modules such as *Windows Build Support*, *macOS Build Support*, or *Linux Build Support*.  
   Select what you need and hit **Done**.

#### Linux (Snap)

```bash
# Start the Hub and add the 6.0.40 version via command line:
unityhub install --version 6.0.40

# If you prefer the GUI, open Unity Hub → Installs → Add → Choose 6.0.40 from the list.
```

> **Linux note:** The Snap package installs to `/snap/unityhub/current` by default; no manual path changes are required.

---

### 4️⃣ Verify the Installation

1. Launch **Unity Hub** → **Installs** tab.  
2. You should now see a row labeled `Unity 6.0.40`.  
3. Click the **Launch** button to start the editor.
4. Inside Unity, go to **Help** → **About Unity** – it should display *Unity 2024.2.0f1 (6.0.40)*.

---

### 5️⃣ Open the "Project" folder in the repo

- The Assets/Bully_AI has all the scenes and scripts Carlos wrote

---

## 🎉 You’re Ready!

- **ML‑Agents** and the game jam project is now installed and ready for training environments, simulations, or contribution to the repo.
- If anything breaks, double‑check each step above and consult the Unity ML‑Agents docs or ask me.  

In order to improve the developer experience for Unity ML-Agents Toolkit, we have added in-editor analytics. Please refer to "Information that is passively collected by Unity" in the [Unity Privacy Policy](https://unity3d.com/legal/privacy-policy).
