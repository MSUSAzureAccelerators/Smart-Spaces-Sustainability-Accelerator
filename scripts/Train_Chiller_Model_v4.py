#!/usr/bin/env python
# coding: utf-8
# %%

# # Train Chiller Lead Time Model
# Depends on: weather, temperature, and setpoint data sources
# 
# Outputs: Weather forecast and chiller lead time forecasts
# 
# Model purpose: to predict lead time needed to reach set point

# %%

# Libraries
import pandas as pd
import sklearn as sk
from sklearn.linear_model import LinearRegression
from sklearn.model_selection import cross_validate
import pickle
from urllib.parse import quote_plus
import sqlalchemy
from azure.appconfiguration import AzureAppConfigurationClient
import json
import os
import numpy as np
from statsmodels.tsa.statespace.sarimax import SARIMAX
from azure.keyvault.secrets import SecretClient
from azure.identity import ManagedIdentityCredential

# ## Load data sources

# %%


# Load weather data
# weather_forecast_filename = "data/weatherextract.csv"

# weather_forecast = pd.read_csv(weather_forecast_filename, low_memory=False,
#                 header=0,
#                 usecols=["_10_Minute_Wind_Gust","Anemometer","Hygrometer","Solar_Radiation_Sensor","Thermometer","datetime"])
# azure sql connect tion string

#Retrieve connection string from app config stored in extended paramaters of adf
with open('activity.json', 'r') as params:
    data = json.load(params)

kv_uri = data['typeProperties']['extendedProperties']['kvURI']

credential = ManagedIdentityCredential()

client = SecretClient(vault_url=kv_uri, credential=credential)

azure_storage_connect_str = client.get_secret('storageConnectString')

storage_connect_str = azure_storage_connect_str.value

azure_sql_connect_str = client.get_secret('sqlConnectString')

sql_connect_str = azure_sql_connect_str.value

conn = sql_connect_str
engine = sqlalchemy.create_engine('mssql+pyodbc:///?odbc_connect={}'.format(conn))

table_name = 'WeatherHistoric'

query = f"SELECT * FROM {table_name}"
weather_data = pd.read_sql(query, engine)


# %%


# Strip time
weather_data["Date"] = pd.to_datetime(weather_data["datetime"]).dt.date
weather_data["Hour"] = pd.to_datetime(weather_data["datetime"]).dt.hour

# Find the last hour in the data
last_hour = weather_data["datetime"].max()
last_temp = weather_data[weather_data["datetime"]==last_hour]["Thermometer"].iloc[0]

# Group by hour for forecast training
weather_data_hourly = weather_data.groupby(["Date","Hour"]).agg(
    gust=("_10_Minute_Wind_Gust", "mean"),
    anemo=("Anemometer", "max"),
    temp=("Thermometer", "mean"),
    sun=("Solar_Radiation_Sensor", "mean"), 
    humid=("Hygrometer", "mean")
).reset_index()

# Only need the last 10 days for hourly
weather_data_hourly = weather_data_hourly[-240:]

# Group the data by day
weather_data_daily = weather_data.groupby("Date").agg(
    gust_max=("_10_Minute_Wind_Gust", "max"),
    anemo_max=("Anemometer", "max"),
    temp_min=("Thermometer", "min"),
    temp_max=("Thermometer", "max"),
    sun_mean=("Solar_Radiation_Sensor", "mean"), # Min is useless, always zero at night
    sun_max=("Solar_Radiation_Sensor", "max"),
    humid_min=("Hygrometer", "min"),
    humid_max=("Hygrometer", "max")
).reset_index()

# Free some memory
del weather_data


# %%


# Load Setpoint Data

table_name = 'SpaceSetPointFinal'

query = f"SELECT Datetime as Datetime, [Point-Value] as Setpoint FROM {table_name}"


# setpoint_data_filename = "data/Bowl_Setpoint_1yr.csv"

# setpoint_data = pd.read_csv(setpoint_data_filename, low_memory=False,
#                 header=0,
#                 names=["Datetime", "Setpoint"])

setpoint_data = pd.read_sql(query, engine)

setpoint_data["Date"] = pd.to_datetime(setpoint_data["Datetime"]).dt.date
setpoint_data["Hour"] = pd.to_datetime(setpoint_data["Datetime"]).dt.hour
setpoint_data['datetime'] = setpoint_data['Datetime'].dt.strftime('%Y-%m-%d %H:00')

# %%


# Group the data by hour
setpoint_hourly = setpoint_data.groupby(["Date","Hour","datetime"]).agg(
    Setpoint_start=("Setpoint", lambda x: x.iloc[0]),
    Setpoint_end=("Setpoint", lambda x: x.iloc[-1])
).reset_index()

setpoint_hourly["datetime"] = pd.to_datetime(setpoint_hourly["datetime"])

# Free some memory
del setpoint_data


# %%


# Find setpoint changes that lower the setpoint
setpoint_hourly["Change"] = (setpoint_hourly["Setpoint_start"] > setpoint_hourly["Setpoint_end"]).astype(int)

setpoint_changes = setpoint_hourly[setpoint_hourly["Change"]==1]
# Filter out setpoint drops that are still above 70
setpoint_changes = setpoint_changes[setpoint_changes["Setpoint_end"] < 71]


# %%


# Load Bowl Temp Data
# bowl_temp_filename = "data/Bowl_Temp_1yr.csv"

# bowl_temp_data = pd.read_csv(bowl_temp_filename, low_memory=False,
#                 header=0,
#                 names=["Datetime","Bowl_temp"])

table_name = 'SpaceTempFinal'

query = f"SELECT Datetime as Datetime, [Point-Value] as Bowl_temp FROM {table_name}"

bowl_temp_data = pd.read_sql(query, engine)


bowl_temp_data["Date"] = pd.to_datetime(bowl_temp_data["Datetime"]).dt.date
bowl_temp_data["Hour"] = pd.to_datetime(bowl_temp_data["Datetime"]).dt.hour
bowl_temp_data['datetime'] = bowl_temp_data['Datetime'].dt.strftime('%Y-%m-%d %H:00')

# %%


# Group the data by hour
bowl_temp_hourly = bowl_temp_data.groupby(["Date","Hour","datetime"]).agg(
    Bowl_temp=("Bowl_temp", "mean")
).reset_index()

bowl_temp_hourly["datetime"] = pd.to_datetime(bowl_temp_hourly["datetime"])

# Free some memory
del bowl_temp_data


# ## Find setpoints and join data

# %%



def find_setpoint_reach_time(row, tolerance=5):
    setpoint_datetime = row["datetime"]
    setpoint_value = row["Setpoint_end"]
    setpoint_start = row["Setpoint_start"]
    setpoint_diff = setpoint_start - setpoint_value
    start_temp = bowl_temp_hourly[bowl_temp_hourly["datetime"] == setpoint_datetime]["Bowl_temp"].max()
    setpoint_48hr_end = setpoint_datetime + pd.Timedelta(72, unit='H')
    # Pull the bowl temps for the next 48 hour period
    bowl_temps = bowl_temp_hourly[(bowl_temp_hourly["datetime"] > setpoint_datetime) & (bowl_temp_hourly["datetime"] <= setpoint_48hr_end)]
    # Find the first temp close to setpoint (if any)
    close_enough = setpoint_value + tolerance
    close_enough = 71 + (start_temp - 71)/2
    close_enough = 72
    #close_enough = max(setpoint_start - math.log(max(setpoint_start - setpoint_value,0), max(start_temp-71,1.001)),72)
    setpoint_hit = bowl_temps[bowl_temps["Bowl_temp"] <= close_enough]
    if setpoint_hit.shape[0] == 0:
        hours_to_setpoint = 73.0
        end_temp = bowl_temps["Bowl_temp"].min()
    else:
        hours_to_setpoint = (setpoint_datetime - setpoint_hit["datetime"].min()) / pd.Timedelta(hours=-1)
        #print(setpoint_hit["datetime"].iloc[0])
        end_temp = setpoint_hit["Bowl_temp"].iloc[0]
    #print(hours_to_setpoint)
    # Record the number of hours to setpoint
    return [hours_to_setpoint, setpoint_diff, start_temp, close_enough]

# For each setpoint event
setpoint_changes[["timing", "setpoint_diff", "start_temp", "target"]] = setpoint_changes.apply(lambda row: find_setpoint_reach_time(row, 20), axis=1, result_type ='expand')


# %%
setpoint_changes["isdaytime"] = 1
setpoint_changes["isdaytime"][setpoint_changes["Hour"]<8] = 0
setpoint_changes["isdaytime"][setpoint_changes["Hour"]>20] = 0

# %%


# Fix the date columns
setpoint_changes["Date"] = pd.to_datetime(setpoint_changes["Date"])
weather_data_daily["Date"] = pd.to_datetime(weather_data_daily["Date"])

#Join to weather

joined_data = pd.merge(setpoint_changes, weather_data_daily, on="Date")
#joined_data = pd.merge(joined_data, )
#joined_data


# %%


training_data = joined_data[joined_data["timing"]<73]
#training_data

# ## Train Model

# %%
#Train model

X = training_data[["isdaytime","anemo_max","gust_max","temp_min", "temp_max", "sun_mean", "sun_max", "humid_min", "humid_max","setpoint_diff","Setpoint_start", "start_temp"]]
import math
y = training_data["timing"].apply(math.log)
reg = LinearRegression().fit(X, y)

# Print the feature coefficients
print(dict(zip(X.columns, reg.coef_)))


# %%
#Evaluate model
r2 = reg.score(X, y)
print(f"R-squared: {r2}, WARNING no test/train split yet due to limited data points, this is not an accurate representation of model performance.")


# %%
from sklearn.model_selection import ShuffleSplit
cv_results = cross_validate(LinearRegression(), X, y, cv=ShuffleSplit(3), scoring='r2')
cv_results['test_score']

# %%

#Save model
#model_file_name = "chiller_model.pkl"

#with open(model_file_name, "wb") as model_file:
#    pickle.dump(reg, model_file)


# %%
# Number of hours forward to forecast, 72 plus enough to reach the end of the 3rd day
forecast_horizon = 72 + (23 - last_hour.hour)
# Create forecast data points
weather_forecast_model = SARIMAX(weather_data_hourly["temp"][0:240], order=(12,1,2)).fit()
weather_forecast = weather_forecast_model.predict(start=240+1, end=240+forecast_horizon).to_frame()

gust_forecast_model = SARIMAX(weather_data_hourly["gust"][0:240], order=(12,1,2)).fit()
gust_forecast = gust_forecast_model.predict(start=240+1, end=240+forecast_horizon)

anemo_forecast_model = SARIMAX(weather_data_hourly["anemo"][0:240], order=(12,1,2)).fit()
anemo_forecast = anemo_forecast_model.predict(start=240+1, end=240+forecast_horizon)

sun_forecast_model = SARIMAX(weather_data_hourly["sun"][0:240], order=(12,1,2),  initialization='approximate_diffuse').fit()
sun_forecast = sun_forecast_model.predict(start=240+1, end=240+forecast_horizon)
sun_forecast = sun_forecast.apply(lambda x: max(x, 0))
sun_forecast = weather_data_hourly["sun"][(-1*forecast_horizon):].to_list()

humid_forecast_model = SARIMAX(weather_data_hourly["humid"][0:240], order=(12,1,2)).fit()
humid_forecast = humid_forecast_model.predict(start=240+1, end=240+forecast_horizon)

forecast_datetimes = []
for i in range(1,weather_forecast.shape[0]+1):
    next_hour = last_hour + pd.Timedelta(i, unit='H')
    forecast_datetimes.append(next_hour)

weather_forecast["datetime"] = forecast_datetimes

# %%
# Save forecast data
weather_forecast = weather_forecast.rename(columns = {"predicted_mean":'Thermometer'})
weather_forecast["date"] = weather_forecast["datetime"].dt.date
weather_forecast.to_sql("weather_forecast", engine, index=False, if_exists='replace', schema='dbo')

# %%
# Create forecast to predict from
weather_forecast = weather_forecast.rename(columns = {"Thermometer":'temp'})
weather_forecast['gust'] = gust_forecast
weather_forecast['anemo'] = anemo_forecast
weather_forecast['sun'] = sun_forecast
weather_forecast['humid'] = humid_forecast


# %%
weather_forecast_daily = weather_forecast.groupby("date").agg(
    gust_max=("gust", "max"),
    anemo_max=("anemo", "max"),
    temp_min=("temp", "min"),
    temp_max=("temp", "max"),
    sun_mean=("sun", "mean"), # Min is useless, always zero at night
    sun_max=("sun", "max"),
    humid_min=("humid", "min"),
    humid_max=("humid", "max")
).reset_index()

# %%
# Skip current day
weather_forecast_daily = weather_forecast_daily[1:]

# %%
# Add other features
weather_forecast_daily["isdaytime"] = 1
weather_forecast_daily["setpoint_diff"] = max(setpoint_hourly["Setpoint_end"].iloc[-1]-70,1)
weather_forecast_daily["Setpoint_start"] = setpoint_hourly["Setpoint_end"].iloc[-1]
weather_forecast_daily["start_temp"] = bowl_temp_hourly["Bowl_temp"].iloc[-1]

# %%
# Create Chiller Lead Time Forecasts
chiller_predictions = pd.DataFrame(np.exp(reg.predict(weather_forecast_daily[["isdaytime","anemo_max","gust_max","temp_min", "temp_max", "sun_mean", "sun_max", "humid_min", "humid_max","setpoint_diff","Setpoint_start", "start_temp"]])), columns=["timing"])
# Limit the values
chiller_predictions["timing"] = chiller_predictions["timing"].apply(lambda x: max(x,1))
chiller_predictions["timing"] = chiller_predictions["timing"].apply(lambda x: min(x,72))

chiller_predictions["date"] = weather_forecast_daily["date"].to_list()

#chiller_predictions.to_csv("chiller_predictions.csv", index=False)

#chiller_lead_forecast


# %%
# Save Chiller Lead Forecasts

chiller_predictions.to_sql("chiller_lead_forecast", engine, index=False, if_exists='replace', schema='dbo')