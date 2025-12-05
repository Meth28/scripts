local supremeCube = Action()

local config = {
    price = 1000,
    storage = 9007,
    cooldown = 120
}

-- Limpieza de iconos y efectos al teletransportar
local function clearAllIconsAndEffects(player)
    -- Quitar iconos del cliente (si tu core soporta iconos)
    if player.removeIcon then
        for id = 0, 255 do
            pcall(function() player:removeIcon(id) end)
        end
        pcall(function() player:sendIcons() end)
    end
    if player.sendCreatureIcon then
        pcall(function() player:sendCreatureIcon(player) end)
    end

    -- Quitar condiciones/efectos comunes
    local CONDS = {
        CONDITION_POISON, CONDITION_FIRE, CONDITION_ENERGY, CONDITION_BLEEDING,
        CONDITION_PARALYZE, CONDITION_HASTE, CONDITION_STRENGTH, CONDITION_OUTFIT,
        CONDITION_INVISIBLE, CONDITION_LIGHT, CONDITION_MANASHIELD, CONDITION_REGENERATION,
        CONDITION_SOUL, CONDITION_DROWN, CONDITION_MUTED, CONDITION_DAZZLED,
        CONDITION_CURSED, CONDITION_FREEZING, CONDITION_PACIFIED, CONDITION_FEARED,
        CONDITION_ROOTED
    }
    for _, c in ipairs(CONDS) do
        pcall(function() player:removeCondition(c) end)
    end

    player:getPosition():sendMagicEffect(CONST_ME_POFF)
end

local ciudades = {
    { name = "Ab'Dendriel", teleport = Position(32732, 31634, 7), requiredLevel = 8 },
    { name = "Ankrahmun", teleport = Position(33194, 32853, 8), requiredLevel = 8 },
    { name = "Carlin", teleport = Position(32360, 31782, 7), requiredLevel = 8 },
    { name = "Darashia", teleport = Position(33213, 32454, 1), requiredLevel = 8 },
    { name = "Edron", teleport = Position(33217, 31814, 8), requiredLevel = 8 },
    { name = "Farmine", teleport = Position(33023, 31521, 11), requiredLevel = 8 },
    { name = "Issavi", teleport = Position(33921, 31477, 5), requiredLevel = 8 },
    { name = "Kazordoon", teleport = Position(32649, 31925, 11), requiredLevel = 8 },
    { name = "Krailos", teleport = Position(33657, 31665, 8), requiredLevel = 8 },
    { name = "Liberty Bay", teleport = Position(32317, 32826, 7), requiredLevel = 8 },
    { name = "Marapur", teleport = Position(33842, 32853, 7), requiredLevel = 8 },
    { name = "Port Hope", teleport = Position(32594, 32745, 7), requiredLevel = 8 },
    { name = "Rathleton", teleport = Position(33594, 31899, 6), requiredLevel = 8 },
    { name = "Roshamuul", teleport = Position(33513, 32363, 6), requiredLevel = 8 },
    { name = "Svargrond", teleport = Position(32212, 31132, 7), requiredLevel = 8 },
    { name = "Thais", teleport = Position(32369, 32241, 7), requiredLevel = 8 },
    { name = "Venore", teleport = Position(32957, 32076, 7), requiredLevel = 8 },
    { name = "Yalahar", teleport = Position(32787, 31276, 7), requiredLevel = 8 },
    { name = "Adventurer Island", teleport = Position(32210, 32300, 6), requiredLevel = 8 }
}

local bosses = {
    { name = "Scarlett Etzel", teleport = Position(33395, 32670, 6), requiredLevel = 250 },
    { name = "Grand Master Oberon", teleport = Position(33294, 31288, 9), requiredLevel = 250 },
    { name = "Drume", teleport = Position(32459, 32502, 7), requiredLevel = 250 },
    { name = "Duke Krule", teleport = Position(32347, 32167, 12), requiredLevel = 250 },
    { name = "Earl Osam", teleport = Position(33263, 31985, 8), requiredLevel = 250 },
    { name = "Count Vlarkorth", teleport = Position(33195, 31690, 8), requiredLevel = 250 },
    { name = "Sir Nictros and Baeloc", teleport = Position(33290, 32474, 9), requiredLevel = 250 },
    { name = "Lord Azaram", teleport = Position(32192, 31819, 8), requiredLevel = 250 },
    { name = "King Zelos", teleport = Position(32172, 31918, 8), requiredLevel = 300 },
    { name = "Faceless Bane", teleport = Position(33618, 32523, 15), requiredLevel = 250 },
    { name = "Dream Scar", teleport = Position(32208, 32093, 13), requiredLevel = 250 },
    { name = "Brain Head", teleport = Position(31977, 32328, 10), requiredLevel = 300 },
    { name = "The Unwelcome", teleport = Position(33608, 31523, 10), requiredLevel = 250 },
    { name = "The Dread Maiden", teleport = Position(33556, 31518, 10), requiredLevel = 250 },
    { name = "The Fear Feaster", teleport = Position(33606, 31494, 10), requiredLevel = 250 },
    { name = "The Pale Worm", teleport = Position(33569, 31446, 10), requiredLevel = 250 },
    { name = "Soul War", teleport = Position(33622, 31432, 10), requiredLevel = 400 },
    { name = "Rotten Blood", teleport = Position(32975, 32401, 9), requiredLevel = 400 },
    { name = "Timira The Many-Headed", teleport = Position(33803, 32705, 7), requiredLevel = 250 },
    { name = "Tentugly's Head", teleport = Position(33795, 31385, 6), requiredLevel = 250 },
    { name = "Ratmiral Blackwhiskersk", teleport = Position(33890, 31197, 7), requiredLevel = 250 },
    { name = "Dragon Pack", teleport = Position(33276, 31059, 13), requiredLevel = 250 },
    { name = "The Primal Menace", teleport = Position(33714, 32799, 14), requiredLevel = 250 },
    { name = "Magma Bubble", teleport = Position(33660, 32897, 14), requiredLevel = 250 },
    { name = "The Monster", teleport = Position(33792, 32579, 12), requiredLevel = 250 },
    { name = "The Baron From Below", teleport = Position(33462, 32268, 15), requiredLevel = 250 },
    { name = "The Count Of The Core", teleport = Position(33323, 32112, 15), requiredLevel = 250 },
    { name = "The Duke Of The Depths", teleport = Position(33275, 32319, 15), requiredLevel = 250 },
    { name = "The Brainstealer", teleport = Position(32053, 31463, 15), requiredLevel = 250 },
    { name = "Sugar Daddy", teleport = Position(33398, 32204, 9), requiredLevel = 250 },
    { name = "The Rootkraken", teleport = Position(33852, 31983, 11), requiredLevel = 250 },
    { name = "Arbaziloth", teleport = Position(33877, 32399, 10), requiredLevel = 300 },
    { name = "Urmahlullu The Immaculate", teleport = Position(33920, 31608, 8), requiredLevel = 250 },
    { name = "Leiden", teleport = Position(32668, 31540, 9), requiredLevel = 250 },
    { name = "Jaul", teleport = Position(33543, 31262, 11), requiredLevel = 250 },
    { name = "Mitmah Vanguard", teleport = Position(34050, 31435, 11), requiredLevel = 100 }
}

local function showTeleportMenu(player, title, list)
    local window = ModalWindow({
        title = title,
        message = "Selecciona un destino.",
    })

    for _, entry in ipairs(list) do
        window:addChoice(entry.name, function(player, button)
            if button.name == "Select" then
                if player:getLevel() < entry.requiredLevel then
                    player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "Necesitas al menos nivel " .. entry.requiredLevel .. " para ir a este lugar.")
                    return
                end

                local fromPos = player:getPosition()
                player:teleportTo(entry.teleport, true)
                player:removeMoneyBank(config.price)

                -- Limpieza tras teleport
                clearAllIconsAndEffects(player)

                fromPos:sendMagicEffect(201)
                player:getPosition():sendMagicEffect(CONST_ME_HOLYAREA)
                player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "Te has teletransportado a " .. entry.name)
                player:setDirection(DIRECTION_SOUTH)
                player:setStorageValue(config.storage, os.time() + config.cooldown)
            end
        end)
    end

    window:addButton("Select")
    window:addButton("Volver")
    window:setDefaultEnterButton(0)
    window:setDefaultEscapeButton(1)
    window:sendToPlayer(player)
end

function supremeCube.onUse(player, item, fromPosition, target, toPosition, isHotkey)
  
    local inPz = player:getTile():hasFlag(TILESTATE_PROTECTIONZONE)
    local inNologout = player:getTile():hasFlag(TILESTATE_NOLOGOUT)
    local inFight = player:isPzLocked() or player:getCondition(CONDITION_INFIGHT, CONDITIONID_DEFAULT)

    if not inPz and inFight then
        player:getPosition():sendMagicEffect(CONST_ME_POFF)
        player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "No puedes usar esto en combate.")
        return false
    end

    if inNologout then
        player:getPosition():sendMagicEffect(CONST_ME_POFF)
        player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "No puedes usar esto en una zona de no logout.")
        return false
    end

    if player:getMoney() + player:getBankBalance() < config.price then
        player:getPosition():sendMagicEffect(CONST_ME_POFF)
        player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "No tienes suficiente dinero.")
        return false
    end

    if player:getStorageValue(config.storage) > os.time() then
        local remaining = player:getStorageValue(config.storage) - os.time()
        player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "Tienes que esperar " .. remaining .. " segundos antes de volver a usarlo.")
        player:getPosition():sendMagicEffect(CONST_ME_AGONY)
        return false
    end

    -- Menú principal: Ciudades o Bosses
    local window = ModalWindow({
        title = "Lunera Cube",
        message = "Selecciona una categoria de destino:",
    })

    window:addChoice("Ciudades", function(player, button)
        if button.name == "Select" then
            showTeleportMenu(player, "Ciudades", ciudades)
        end
    end)

    window:addChoice("Bosses", function(player, button)
        if button.name == "Select" then
            showTeleportMenu(player, "Bosses", bosses)
        end
    end)

	-- Agregar opción para teletransportar a la casa si el jugador tiene una
	local house = player:getHouse()
	if house then
		window:addChoice("House", function(player, button, choice)
			if button.name == "Select" then
				if player:getStorageValue(config.storage) <= os.time() then
					local housePosition = house:getExitPosition()
					if housePosition then
						local fromPosition = player:getPosition()
						player:teleportTo(housePosition, true)
						player:removeMoneyBank(config.price)

                        -- Limpieza tras teleport
                        clearAllIconsAndEffects(player)

						fromPosition:sendMagicEffect(201)
						player:getPosition():sendMagicEffect(CONST_ME_HOLYAREA)
						player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "You were teleported to your house.")
						player:setDirection(DIRECTION_SOUTH)
						player:setStorageValue(config.storage, os.time() + config.cooldown)
					else
						player:getPosition():sendMagicEffect(CONST_ME_POFF)
						player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "Your house entry position is invalid.")
					end
				else
					player:getPosition():sendMagicEffect(CONST_ME_POFF)
					player:sendTextMessage(MESSAGE_EVENT_ADVANCE, "You need to wait before using this again.")
				end
			end
			return true
		end)
	end

    window:addButton("Select")
    window:addButton("Cerrar")
    window:setDefaultEnterButton(0)
    window:setDefaultEscapeButton(1)
    window:sendToPlayer(player)

    return true
end

supremeCube:id(31633)
supremeCube:register()
