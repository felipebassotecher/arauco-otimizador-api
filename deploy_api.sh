# --------------------------------------------------
# Resources
# --------------------------------------------------


# sam deploy \
# 	--template-file resources_cognito.yml \
# 	--s3-bucket arauco-$ENVIRONMENT-deploys \
# 	--s3-prefix api-resources/stack-api-resources-cognito \
# 	--region $AWS_REGION \
# 	--capabilities CAPABILITY_IAM \
# 	--stack-name stack-api-resources-cognito \
# 	--parameter-overrides ParameterKey=ENV,ParameterValue=$ENVIRONMENT \
# 	--no-fail-on-empty-changeset

sam deploy \
	--template-file resources.yml \
	--s3-bucket arauco-$ENVIRONMENT-deploys \
	--s3-prefix api-resources/stack-api-resources \
	--region $AWS_REGION \
	--capabilities CAPABILITY_NAMED_IAM \
	--stack-name stack-api-resources \
	--parameter-overrides ParameterKey=ENV,ParameterValue=$ENVIRONMENT \
	--no-fail-on-empty-changeset

# --------------------------------------------------
# Functions
# --------------------------------------------------

# cd ../Arauco.Otimizador.Function.EmailOutbound
# dotnet restore
# dotnet lambda deploy-serverless \
#     --configuration Release \
#     --region $AWS_REGION \
#     --s3-bucket arauco-$ENVIRONMENT-deploys \
#     --s3-prefix api/stack-email-outbound-function \
#     --stack-name stack-email-outbound-function \
#     --template serverless.yml \
#     --template-parameters "ENV=$ENVIRONMENT"


# --------------------------------------------------
# WebApi
# --------------------------------------------------

cd ../Arauco.Otimizador.WebApi
dotnet restore
dotnet lambda deploy-serverless \
    --configuration Release \
    --framework net10.0 \
    --region $AWS_REGION \
    --s3-bucket arauco-$ENVIRONMENT-deploys \
    --s3-prefix api/stack-otimizador-api \
    --stack-name stack-otimizador-api \
    --template serverless.yml \
    --template-parameters "ENV=$ENVIRONMENT;APIDOMAIN=$APIDOMAIN"
