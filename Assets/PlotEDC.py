import matplotlib
matplotlib.use("Agg")  # Use non-GUI backend to prevent font rebuilding

import matplotlib.pyplot as plt
import numpy as np
import os

# Suppress font cache log
matplotlib.rcParams['font.family'] = 'DejaVu Sans'  # Ensures cached font is used

# Define file paths
script_dir = os.path.dirname(__file__)
data_file = os.path.join(script_dir, "EDC_Data.csv")
output_image = os.path.join(script_dir, "EDC_Graph.png")

# Load data
if not os.path.exists(data_file):
    print(f"❌ Missing EDC data file: {data_file}")
    exit(1)

data = np.loadtxt(data_file)

# Plot EDC
plt.figure(figsize=(6, 4))
plt.plot(data, color='blue', label="Energy Decay Curve")
plt.xlabel("Samples")
plt.ylabel("Energy")
plt.title("Energy Decay Curve (EDC)")
plt.legend()
plt.grid(True)

# Save the image
plt.savefig(output_image)
plt.close()
print(f"✅ EDC Graph Saved: {output_image}")
