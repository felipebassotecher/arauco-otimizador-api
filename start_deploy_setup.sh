AWS_REGION="sa-east-1"
BRANCH=$(git branch --show-current)
REPOSITORY_NAME=$(basename $(git remote get-url origin))

IFS='-' read -ra ADDR <<< "$REPOSITORY_NAME"
REPO_PROJECT=${ADDR[0]}-${ADDR[1]}

ALLOWEDBRANCHES=("dev" "test" "master")

declare "envs_dev=dev"
declare "envs_qa=test"
declare "envs_prod=master"

arrayGet(){
    local array=$1 index=$2
    local i="${array}_$index"
    printf '%s' "${!i}"
}

if [[ ! " ${ALLOWEDBRANCHES[*]} " =~ " ${BRANCH} " ]]; then
    echo "Is not possible to deploy current branch - try with dev, test or master"
    exit 1
fi


if result=$(aws ssm get-parameter --name Env --region $AWS_REGION 2>&1); then
    ENVIRONMENT=$(echo $result | python -c "import json,sys;obj=json.load(sys.stdin);deps = obj['Parameter'];print(deps['Value'])")
else
    rc=$?
    ENV_ERROR=$result
fi

if result=$(aws ssm get-parameter --name Project --region $AWS_REGION 2>&1); then
    PROJECT=$(echo $result | python -c "import json,sys;obj=json.load(sys.stdin);deps = obj['Parameter'];print(deps['Value'])")
else
    rc=$?
    PROJ_ERROR=$result
fi


if [[ ! -z "$ENV_ERROR" || ! -z "$PROJ_ERROR" ]] ; then
    echo $ENV_ERROR $PROJ_ERROR
    exit 1
fi

if [[ ! "$REPO_PROJECT" = "$PROJECT" ]] ; then
    echo "You are trying to deploy the wrong project"
    exit 1
fi

if [[ ! "$BRANCH" = "$(arrayGet envs "$ENVIRONMENT")" ]] ; then
    echo "You are trying to deploy in the wrong enviroment"
    exit 1
fi

###########################Script Deploy############################

sam deploy --template-file setup.yml --s3-bucket arauco-$ENVIRONMENT-deploys --s3-prefix setup/stack-setup --region $AWS_REGION --stack-name stack-setup --parameter-overrides ParameterKey=ENV,ParameterValue=$ENVIRONMENT --no-fail-on-empty-changeset --capabilities CAPABILITY_NAMED_IAM
