/*********
  Ethan Young
  
  Compatible with Python program available in GitHub repo
*********/

#include <ESP8266WiFi.h>
#include <WiFiManager.h>         // https://github.com/tzapu/WiFiManager

unsigned long seconds = 0;
const int flowsensor = 0;


// Set web server port number to 80
WiFiServer server(80);

// Variable to store the HTTP request
String header;


void setup() {
  Serial.begin(9600);

  pinMode(flowsensor, INPUT);
  

  // WiFiManager
  // Local intialization. Once its business is done, there is no need to keep it around
  WiFiManager wifiManager;

  // set custom ip for portal
  wifiManager.setAPStaticIPConfig(IPAddress(10,0,1,1), IPAddress(10,0,1,1), IPAddress(255,255,255,0));


  // fetches ssid and pass from eeprom and tries to connect
  // if it does not connect it starts an access point with the specified name
  // here  "AutoConnectAP"
  // and goes into a blocking loop awaiting configuration
  wifiManager.autoConnect("AutoConnectAP");

  //To connect:
  // 1) Connect to "AutoConnectAP"
  // 2) Open browser and visit default IP 192.168.4.1 (May be a pop up)
  // 3) Enter credentials and you are good to go
  
  // if you get here you have connected to the WiFi
  Serial.println("Connected.");
  Serial.println(WiFi.localIP());

  server.begin();

}

void loop(){

  WiFiClient client = server.available();   // Listen for incoming clients

  if (client) {                             // If a new client connects,
    Serial.println("New Client.");          // print a message out in the serial port
    String currentLine = "";                // make a String to hold incoming data from the client
    
    while (client.connected()) {            // loop while the client's connected
        seconds = pulseIn(flowsensor, HIGH); // read in pulse width from sensor
        
        if (seconds > 0) {
          double flow_rate = 0.5*100000 / seconds; // Place holder flow rate calc
          
          Serial.println(String(flow_rate));
          client.println(String(flow_rate) + ","); // Protocol parses for ',' in server

          delay(100);
        }
        
        delay(50);
    }
                  
    // Close the connection
    client.stop();
    Serial.println("Client disconnected.");
    Serial.println("");
  }
}
