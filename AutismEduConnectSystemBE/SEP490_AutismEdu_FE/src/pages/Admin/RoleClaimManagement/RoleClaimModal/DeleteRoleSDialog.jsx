import RemoveIcon from '@mui/icons-material/Remove';
import { IconButton } from '@mui/material';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import * as React from 'react';
function DeleteRoleSDialog({ handleDeleteUserFromRole, id }) {
    const [open, setOpen] = React.useState(false);

    const handleClickOpen = () => {
        setOpen(true);
    };

    const handleSubmit = async () => {
        console.log("Submitting delete for id:", id);

        await handleDeleteUserFromRole(id);

        handleClose();
    }
    const handleClose = () => {
        setOpen(false);
    };
    return (
        <React.Fragment>
            <IconButton onClick={handleClickOpen}>
                <RemoveIcon sx={{ color: "#FF8343" }} />
            </IconButton>
            <Dialog
                open={open}
                onClose={handleClose}
                aria-labelledby="alert-dialog-title1"
                aria-describedby="alert-dialog-description"
            >
                <DialogTitle id="alert-dialog-title1">
                    {"Do you want remove this role"}
                </DialogTitle>
                <DialogActions>
                    <Button onClick={handleSubmit}>Delete</Button>
                    <Button onClick={handleClose} autoFocus>
                        Cancle
                    </Button>
                </DialogActions>
            </Dialog>
        </React.Fragment>
    )
}

export default DeleteRoleSDialog
