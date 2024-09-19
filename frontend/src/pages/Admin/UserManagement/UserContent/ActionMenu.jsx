import { IconButton, Menu, MenuItem } from '@mui/material'
import React from 'react'
import MoreVertIcon from '@mui/icons-material/MoreVert';
import UserClaimModal from '../UserClaimModal';
import UserRoleModal from '../UserRoleModal';
<<<<<<< HEAD
function ActionMenu() {
=======
function ActionMenu({ currentUser }) {
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const [anchorEl, setAnchorEl] = React.useState(null);
    const open = Boolean(anchorEl);
    const handleClick = (event) => {
        setAnchorEl(event.currentTarget);
    };
    const handleClose = () => {
        setAnchorEl(null);
    };
    return (
        <div>
            <IconButton
                id="demo-positioned-button"
                aria-controls={open ? 'demo-positioned-menu' : undefined}
                aria-haspopup="true"
                aria-expanded={open ? 'true' : undefined}
                onClick={handleClick}
            >
                <MoreVertIcon />
            </IconButton>
            <Menu
                id="demo-positioned-menu"
                aria-labelledby="demo-positioned-button"
                anchorEl={anchorEl}
                open={open}
                onClose={handleClose}
                anchorOrigin={{
                    vertical: 'top',
                    horizontal: 'left',
                }}
                transformOrigin={{
                    vertical: 'top',
                    horizontal: 'left',
                }}
            >
<<<<<<< HEAD
                <UserClaimModal handleCloseMenu={handleClose}/>
                <UserRoleModal handleCloseMenu={handleClose} />
=======
                <UserClaimModal handleCloseMenu={handleClose} currentUser={currentUser} />
                <UserRoleModal handleCloseMenu={handleClose} currentUser={currentUser}/>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
            </Menu>
        </div>
    )
}

export default ActionMenu
