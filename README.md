# Automated Goods Received Note (GRN) System

## Overview

AutoGRN Conveyor is an industrial automation system designed to automate the goods receiving process for component reels. The system uses machine vision, barcode scanning, and ERP integration to capture reel information, verify data with the ERP system, and print GRN stickers automatically on a conveyor line.

### Key Capabilities

- **Automated Reel Scanning**: Triple-camera system captures and decodes barcodes/datamatrix codes from moving reels
- **Multi-Vendor Support**: Configurable vendor profiles with customizable data extraction rules
- **ERP Integration**: Real-time communication with ERP system for GRN generation
- **Automated Printing**: Brady printer integration for GRN sticker printing
- **Barcode Verification**: End-of-line scanner verifies printed stickers
- **Password Protection**: Secure access with configurable password management
- **Conveyor Control**: PLC integration for automated material handling

---

## System Architecture

### Hardware Components

1. **Vision System**
   - 3x Basler industrial cameras
   - Cognex VisionPro SDK for barcode/datamatrix/QR code reading

2. **I/O Control**
   - Advantech PCIE-1730 I/O card for sensor monitoring and relay control
   - Multiple proximity sensors for reel detection
   - Tower light for status indication

3. **Printing System**
   - Brady printer with CodeSoft integration
   - Barcode scanner (Serial COM4) for verification

4. **Conveyor System**
   - Motor control via relay outputs
   - Position sensors for reel tracking

## Prerequisites

### Framework & Runtime
- .NET Framework 4.x or higher
- Windows 10

### Required Libraries & SDKs

```
Core Dependencies:
├── Newtonsoft.Json                    # JSON serialization
├── Basler.Pylon                       # Camera SDK
├── Cognex.VisionPro                   # Barcode reading
├── Cognex.VisionPro.ID                # 1D/2D code decoding
├── Cognex.VisionPro.ImageFile         # Image processing
├── LabelManager2                      # Brady printer control
├── Automation.BDaq                    # Advantech I/O control
└── ERP DLL                            # ERP integration
```

### Hardware Requirements
- Intel Core i5 or higher
- 8GB RAM minimum
- SSD recommended for image processing
- PCIe slot for Advantech I/O card
- USB 3.0 ports for cameras
- Serial port (or USB-Serial adapter) for barcode scanner


## Configuration

### Vendor Registration

Each vendor has unique barcode format. Register vendors before processing:

1. Launch application and click **Settings**
2. Click **Register** button
3. Scan sample reel barcode with scanner
4. Configure vendor profile:
   - **Vendor Name**: Unique identifier
   - **Splitter**: Auto-detected delimiter (don't modify if detected)
   - **Capture Time (ms)**: Camera trigger delay (default: 800ms)
   - **Print Time (ms)**: Print trigger delay (default: 700ms)
   - **Code Type**: DataMatrix or QR

5. Map barcode fields:
   - **Position**: Field order in scanned string
   - **Starting**: Start character index
   - **Ending**: End character index

6. Mandatory fields: Part Number, Quantity, Manufacturer, Vendor Code

### Vendor Profile Example

For barcode: `1|p-06|2|P28024551|3|19SCV*70MN*D971305001|4|Q4000...`

Configuration:
- Part No: Position 2, Start 2, End (blank)
- Quantity: Position 4, Start 2, End (blank)
- Manufacturer: Position 6, Start 3, End (blank)
- Vendor Code: N/A (manual entry: D0001)

---

## Usage

### Standard Operation

1. **Start System**:
   - Power on all hardware
   - Launch "AutoGRN Conveyor"
   - Verify ERP connection (should show "Connected" in green)
   - Select vendor from dropdown
   - Click **Start**

2. **Process Reels**:
   - Place reel on conveyor with barcode facing cameras
   - System automatically:
     - Captures image
     - Decodes barcode
     - Sends data to ERP
     - Prints GRN sticker
     - Verifies printed barcode

3. **Monitor Status**:
   - Camera images display in top panels
   - Event log shows all operations with timestamps
   - Tower light indicates system status

### Emergency Stop

Press **Emergency Stop** button to immediately halt all operations and stop the conveyor.

---

## Project Structure

```
AutoGRN_Conveyor/
├── Form1.cs                    # Main application logic
├── Form1.Designer.cs           # Main form UI design
├── Reg_Form.cs                 # Vendor registration form
├── Reg_Form.Designer.cs        # Registration form UI
├── Password.cs                 # Authentication dialog
├── Password.Designer.cs        # Password form UI
├── Program.cs                  # Application entry point
└── C:\AutoGRN_config\          # Configuration directory
    ├── Config.txt              # ERP credentials
    ├── Password.txt            # User password
    ├── vendor.txt              # Vendor profiles
    └── log.txt                 # Application logs
```

## Appendix

### Wiring Diagram
See `AUTO_GRN.pdf` for complete electrical wiring diagram

### Operating Manual
See `AutoGRN_Operating_Manual.pdf` for detailed user guide with screenshots

### Supported Barcode Types
- 1D Barcodes (Code 39, Code 128, EAN, UPC)
- 2D DataMatrix
- QR Codes
