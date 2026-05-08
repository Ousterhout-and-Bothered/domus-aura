provider "aws" {
  region  = "us-east-1"
  profile = "domus-aura-tf"

  default_tags {
    tags = {
      Project     = "domus-aura"
      ManagedBy   = "terraform"
      Environment = "production"
    }
  }
}