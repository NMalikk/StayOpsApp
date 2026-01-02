@echo off
echo Starting Kubernetes SQL Deployment:


echo Creating mssql-secret.
kubectl create secret generic mssql --from-literal=MSSQL_SA_PASSWORD="YourStrong!Passw0rd"


echo Applying PVC.
kubectl apply -f pvc.yaml


echo Applying SQL Deployment.
kubectl apply -f sqldeployment.yaml

echo.
echo Deployment commands sent successfully.
pause