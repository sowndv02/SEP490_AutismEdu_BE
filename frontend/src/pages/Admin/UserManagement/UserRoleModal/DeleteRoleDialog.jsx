import RemoveIcon from '@mui/icons-material/Remove';
import { IconButton } from '@mui/material';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import * as React from 'react';
<<<<<<< HEAD
function DeleteRoleDialog() {
=======
function DeleteRoleDialog({ handleRemoveRole, id }) {
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
    const [open, setOpen] = React.useState(false);

    const handleClickOpen = () => {
        setOpen(true);
    };

<<<<<<< HEAD
=======
    const handleSubmit = () => {
        handleRemoveRole(id);
        handleClose();
    }
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
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
                aria-labelledby="alert-dialog-title"
                aria-describedby="alert-dialog-description"
            >
                <DialogTitle id="alert-dialog-title">
                    {"Do you want remove this claim"}
                </DialogTitle>
                <DialogActions>
<<<<<<< HEAD
                    <Button onClick={handleClose}>Delete</Button>
=======
                    <Button onClick={handleSubmit}>Delete</Button>
>>>>>>> 5598c1832bd23a189aad54969380111a502c987f
                    <Button onClick={handleClose} autoFocus>
                        Cancle
                    </Button>
                </DialogActions>
            </Dialog>
        </React.Fragment>
    )
}

export default DeleteRoleDialog
