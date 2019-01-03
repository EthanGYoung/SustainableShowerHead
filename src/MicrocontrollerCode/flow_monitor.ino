#include <ESP8266WiFi.h>
#include <WiFiManager.h>         // https://github.com/tzapu/WiFiManager
#include <ESP8266HTTPClient.h>
#include <ArduinoJson.h>;
#include <ArduinoOTA.h>
#include "Secrets.h"

// Server Configuration
const char* END_POINT = END_POINT_SECRET;
const char* HOST =  HOST_SECRET;
const int HTTPS_PORT = 443;
const char FINGERPRINT[] PROGMEM = FINGERPRINT_SECRET; // Fingerprint for host website
HTTPClient client;

// Device Metadata (Move to separate file)
const int SOFTWARE_VERSION = 0.1;
const int DEVICE_ID = 00000001;

// OTA Settings
const char* OTAName = OTA_USERNAME_SECRET;
const char* OTAPassword = OTA_PASSWORD_SECRET;

// Action Configuration
#define FLOW_DATA     "Flow_Data"
#define IS_ALIVE      "Is_Alive"

// HW Configuration
const int flowsensor = 0; // Pin number of sensor

// Flow sensing
unsigned long seconds;
unsigned long start_time;
unsigned long curr_time;
#define TIME_BTW_MSGS 500 // Time in milliseconds between flow data sending




/*________________________________________________________________SETUP___________________________________________________________________________________*/

/* Look into time method takes to run */
void setup() {
  Serial.begin(9600);
  
  startWiFi();
  
  configureHostConnection();
  
  startOTA();
  
  configHW();
  
  sendIsAlive();
  
}

/*_________________________________________________________________LOOP___________________________________________________________________________________*/
void loop() {
  ArduinoOTA.handle();

  // Reinitialize variables
  start_time = millis();
  curr_time = millis();
  seconds = 0;

  // Gather data from sensor
  while ((curr_time - start_time) < TIME_BTW_MSGS) {
    seconds += pulseIn(flowsensor, HIGH, TIME_BTW_MSGS);
    curr_time = millis();
  }

  double rate = calculateFlowRate(seconds, curr_time - start_time);

  if (rate > 0) {
    // Send post request to send data
    sendPostRequest(FLOW_DATA, String(rate));
    payloadHandled();
  }

  
}

/*____________________________________________________________HELPER_FUNCTIONS____________________________________________________________________________*/

double calculateFlowRate(unsigned long seconds, unsigned long time_diff) {
  if (time_diff == 0) {
    return 0;
  } else {
    return ((double) seconds) / time_diff; // Place holder for actual calculation
  }
}

void configHW() {
  pinMode(flowsensor, INPUT);
}

void startWiFi() {
  // WiFiManager
  // Local intialization. Once its business is done, there is no need to keep it around
  WiFiManager wifiManager;

  //if you want to erase all the stored information
  //wifiManager.resetSettings();

  // Connect to AutoConnectAP to set up username and password
  wifiManager.autoConnect("AutoConnectAP");
  
  // if you get here you have connected to the WiFi
  Serial.println("Connected.");
}

void configureHostConnection() {
  client.begin(HOST, HTTPS_PORT, END_POINT, true, FINGERPRINT);
}

/* Tells cloud that this device booted up */
void sendIsAlive() {
  sendGetRequest(IS_ALIVE);
  payloadHandled();
  
  // Future: Handle updating state to match what is returned
}

/* 
 *  Formats GET request and sends it to END_POINT
 *  Return: https response code 
 */
int sendGetRequest(String action) {
  Serial.println("Sending GET Request");
  return client.GET();
}

/* 
 *  Formats POST request and sends it to END_POINT
 *  Return: https response code 
 */
int sendPostRequest(String action, String value) {
  client.addHeader("Content-Type", "application/json");
  client.addHeader("Accept", "application/json");

  String buffer = constructDataPayload(action, value);
  
  /* Debug */
  Serial.print("Request being sent: ");
  Serial.println(buffer);

  return client.POST(buffer);
}

String getHTTPResponse() {
  return client.getString();
}

/* Called after response payload is handled */
void payloadHandled() {
  client.end();
}

String constructDataPayload(String action, String value) {
  /* Create JSON payload
       {
         "deviceId": "00000001",
         "softwareVersion": "1.0"
         "Action": "Action",
         "Value": "Value_Associated_With_Action",
       }
  */
  StaticJsonBuffer<200> jsonBuffer;
  JsonObject& req = jsonBuffer.createObject();
  
  req["deviceID"] = DEVICE_ID;
  req["sofwareVersion"] = SOFTWARE_VERSION;
  req["Action"] = action;
  req["Value"] = value;
  
  String buffer;
  req.printTo(buffer);

  return buffer;
}

void startOTA() { // Start the OTA service
  ArduinoOTA.setHostname(OTAName);
  ArduinoOTA.setPassword(OTAPassword);

  ArduinoOTA.onStart([]() {
    Serial.println("Start");
  });
  ArduinoOTA.onEnd([]() {
    Serial.println("\r\nEnd");
  });
  ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
    Serial.printf("Progress: %u%%\r", (progress / (total / 100)));
  });
  ArduinoOTA.onError([](ota_error_t error) {
    Serial.printf("Error[%u]: ", error);
    if (error == OTA_AUTH_ERROR) Serial.println("Auth Failed");
    else if (error == OTA_BEGIN_ERROR) Serial.println("Begin Failed");
    else if (error == OTA_CONNECT_ERROR) Serial.println("Connect Failed");
    else if (error == OTA_RECEIVE_ERROR) Serial.println("Receive Failed");
    else if (error == OTA_END_ERROR) Serial.println("End Failed");
  });
  ArduinoOTA.begin();
  Serial.println("OTA ready\r\n");
}
