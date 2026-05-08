# -------- ECR repository URLs --------
# Used by GitHub Actions to push images, and by EC2 to pull them.

output "ecr_api_repository_url" {
  description = "URL of the API ECR repository (push target for CI)."
  value       = aws_ecr_repository.api.repository_url
}

output "ecr_frontend_repository_url" {
  description = "URL of the frontend ECR repository (push target for CI)."
  value       = aws_ecr_repository.frontend.repository_url
}

output "ecr_registry" {
  description = "ECR registry hostname for docker login."
  value       = "${data.aws_caller_identity.current.account_id}.dkr.ecr.us-east-1.amazonaws.com"
}

# -------- GitHub Actions credentials --------
# Paste these into GitHub repo secrets. The secret value is shown only when
# explicitly retrieved with `terraform output -raw github_actions_secret_access_key`.

output "github_actions_access_key_id" {
  description = "Access key ID for the github-actions-deployer IAM user."
  value       = aws_iam_access_key.github_actions.id
}

output "github_actions_secret_access_key" {
  description = "Secret access key for the github-actions-deployer IAM user. Sensitive."
  value       = aws_iam_access_key.github_actions.secret
  sensitive   = true
}