# Snipet
```sh
az login
az account set --subscription ""

az group create --name "koudenpa-mackerel" --location "japaneast"
az group delete --name "koudenpa-mackerel"

az group deployment validate --resource-group "koudenpa-mackerel" --template-file "template.json"
az group deployment create --resource-group "koudenpa-mackerel" --template-file "template.json"
```

```sh

```