import { Box, MenuItem, Modal, Tab, Tabs, Typography } from '@mui/material';
import { useState } from 'react';
import ClaimTable from './ClaimTable';
import MyClaim from './MyClaim';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 600,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4,
};
function UserClaimModal({ handleCloseMenu, currentUser }) {
    const [open, setOpen] = useState(false);
    const [tab, setTab] = useState(0);
    const [claims, setClaims] = useState(null);
    const [pagination, setPagination] = useState(null);
    const [selected, setSelected] = useState([]);
    const [currentPage, setCurrentPage] = useState(1)

    const handleOpen = () => {
        setOpen(true)
    };
    const handleClose = () => {
        handleCloseMenu()
        setOpen(false);
    }

    const handleChangeTab = (event, newValue) => {
        setTab(newValue);
    };
    return (
        <div>
            <MenuItem onClick={() => { handleOpen(); }}>Manage Claim</MenuItem>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style} >
                    <Typography id="modal-modal-title" variant="h6" component="h2">
                        Claim of {currentUser.fullName}
                    </Typography>
                    <Tabs value={tab} onChange={handleChangeTab} aria-label="basic tabs example">
                        <Tab label={`${currentUser.fullName}'s Claims`} />
                        <Tab label="Add Claims" />
                    </Tabs>

                    {
                        tab === 0 ? <MyClaim currentUser={currentUser} /> : <ClaimTable claims={claims} setClaims={setClaims} pagination={pagination}
                            setPagination={setPagination}
                            userId={currentUser.id}
                            selected={selected}
                            setSelected={setSelected}
                            setTab={setTab}
                            currentPage={currentPage}
                            setCurrentPage={setCurrentPage}
                        />
                    }
                </Box>
            </Modal>
        </div>
    )
}

export default UserClaimModal
