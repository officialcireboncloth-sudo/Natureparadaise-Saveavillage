# Animal housing construction at Lumber Store

Open Lumber Store, choose **Bangun**, then select **Barn** or **Coop**. The order opens the existing level 1 placement preview. Select a valid location and confirm to pay the configured gold/materials and dispatch the builder. Cancelling placement does not pay for a project.

- Lumber Store retains the shared construction catalog, placement rules, costs, construction worker, and upgrade service.
- Empty animal-housing Property Sites no longer display a build marker or open the old construction menu. Legacy fixed Building Sites no longer offer their level 1 animal-housing preview through world interaction.
- The player build menu filters out animal housing, including when an old scene supplies a mixed catalog. Other entries retain their original catalog index, so Field/fish pond construction remains available.
- Carpenter guidance and the animal purchase requirement point to Lumber Store.
- Existing housing IDs, positions, levels, residents, saved construction jobs, relocation, demolition, and completion use the existing state controllers. No save schema or scene migration is required.
- The construction service rejects non-Lumber shop fronts. Existing Lumber Store buttons support mouse/touch; keyboard selection remains available.

Native validation runs in an isolated project copy using the authored Lumber Store prefab: build buttons, legacy filtering, invalid and valid paid placement, duplicate confirmation, builder dispatch, JSON save/load into a fresh site, completion, upgrade access, back/close, and insufficient funds. Main Map is not opened or saved. Worker completion is invoked explicitly for this routing test; worker-day timing is unchanged.
