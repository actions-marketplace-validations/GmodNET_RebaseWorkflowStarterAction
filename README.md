# RebaseWorkflowStarterAction

 A GitHub Action to automatically start rebase workflow

## Description

This action is designed to be run on `push` event and trigger auto rebase for relevant Pull Requests.

## Usage

Create a rebase Github workflow with trigger type `workflow_dispatch`. Here is an example of workflow we are using in our projects:

```yml
name: Rebase Pull Request Workflow

on:
  workflow_dispatch:
    inputs:
      prNumber:
        description: 'A number of the Pull Request to rebase'
        required: true

jobs:
  rebase_and_push:
    name: Try rebase and push
    runs-on: ubuntu-latest

    steps:
      - name: Generate GitHub App token
        id: generate_token
        uses: tibdex/github-app-token@v1.3.0
        with:
          app_id: ${{ secrets.GMODNET_GITHUB_BOT_ID }}
          private_key: ${{ secrets.GMODNET_GITHUB_BOT_KEY }}

      - name: Configere Git User information
        run: |
             git config --global user.name "GmodNET GitHub Bot"
             git config --global user.email support@gmodnet.xyz

      - name: Extract branch name
        uses: nelonoel/branch-name@v1.0.1

      - name: Checkout
        env:
          GITHUB_TOKEN: ${{ steps.generate_token.outputs.token }}
        run: |
             gh repo clone ${{ github.repository }} ./
             git checkout ${{ env.BRANCH_NAME }}
             gh pr checkout ${{ github.event.inputs.prNumber }}

      - name: Rebase and push
        id: rebase_and_push
        continue-on-error: true
        run: |
             git rebase ${{ env.BRANCH_NAME }}
             git remote set-url origin https://x-access-token:${{ steps.generate_token.outputs.token }}@github.com/${{ github.repository }}.git
             git push --force

      - name: Notify if rebase was unsuccessful
        if: steps.rebase_and_push.outcome == 'failure'
        env:
          GITHUB_TOKEN: ${{ steps.generate_token.outputs.token }}
        run: gh pr comment ${{ github.event.inputs.prNumber }} --body "Automatic rebase to branch '${{ env.BRANCH_NAME }}' has failed. Manual rebase is required."
```

Such workflow must have single input called `prNumber`, which will receive a number of Pull Request to rebase.

Then add this action to a job which runs on `push` event:

```yml
- name: Trigger rebase workflow
  uses: GmodNET/RebaseWorkflowStarterAction@v1
  with:
    token: token # A GitHub API token to authorize with. Token must have read access to the repository and able to dispatch repository workflows
    worflow_id: rebase-pr-workflow.yml # A file name of rebase workflow
    pr_label_name: laben-name # Name of the GitHub label for auto rebase
```

On run, action will get all open PRs which have current branch as a target and, if they have specified label, trigger rebase workflow for them. Rebase workflow will be triggered from the current branch.
