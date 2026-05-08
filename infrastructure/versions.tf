terraform {
  required_version = ">= 1.10.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.70"
    }
  }

  backend "s3" {
    bucket         = "domus-aura-tfstate-608752952798"
    key            = "main/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "domus-aura-tflock"
    encrypt        = true
  }
}