import React, { useEffect, useState } from 'react';
import { Box, Button, Dialog, DialogTitle, DialogContent, DialogActions, Select, MenuItem, Checkbox, FormControl, InputLabel, Stack, Typography, Divider } from '@mui/material';
import services from '~/plugins/services';
import Autocomplete from '@mui/material/Autocomplete';
import TextField from '@mui/material/TextField';
import { enqueueSnackbar } from 'notistack';

function ExerciseAdd({ openModal, handleCloseModal, exerciseTypes, selectedList, setSelectedList, selectedClone, setSelectedClone }) {
    const [newData, setNewData] = useState({
        // exerciseTypeId: exerciseTypes[0]?.id || 1,
        exerciseTypeId: selectedList[0]?.exerciseTypeId || 1,
        exerciseIds: selectedList[0]?.exerciseIds ?? []
    });

    const [exercises, setExercises] = useState([]);

    useEffect(() => {
        if (newData.exerciseTypeId !== exerciseTypes[0].id) {
            const exist = selectedList?.find((s) => s.exerciseTypeId === newData.exerciseTypeId);
            if (exist) {
                setNewData((prev) => ({
                    ...prev,
                    exerciseIds: exist.exerciseIds
                }));
            }
        }
        handleGetExerciseByType();
    }, [newData.exerciseTypeId]);

    const handleGetExerciseByType = async () => {
        try {
            await services.ExerciseManagementAPI.getExerciseByType(newData.exerciseTypeId, (res) => {
                if (res?.result) {
                    setExercises(res.result);
                }
            }, (error) => {
                enqueueSnackbar(error.error[0], { variant: 'error' });
                console.log(error);
            }, { search: '', orderBy: 'createdDate', sort: 'desc' });
        } catch (error) {
            console.log(error);
        }
    };

    const handleChangeData = (e) => {
        const { name, value, checked } = e.target;

        if (name === 'exerciseTypeId') {
            setNewData({ [name]: parseInt(value), exerciseIds: selectedList?.find((s) => s?.exerciseTypeId === parseInt(value))?.exerciseIds ?? [] });
        } else if (name === 'exerciseIds') {
            const exerciseId = parseInt(value);

            setNewData((prev) => {
                const { exerciseIds } = prev;

                if (checked) {
                    return { ...prev, exerciseIds: [...exerciseIds, exerciseId] };
                } else {
                    return { ...prev, exerciseIds: exerciseIds.filter(id => id !== exerciseId) };
                }
            });
        }
    };


    const handleAdd = () => {

        if (newData.exerciseIds.length === 0) {

            const updatedSelectedList = selectedList?.filter(s => s.exerciseTypeId !== newData.exerciseTypeId);
            const updatedSelectedClone = selectedClone?.filter(s => s.eType.id !== newData.exerciseTypeId);

            setSelectedList(updatedSelectedList);
            setSelectedClone(updatedSelectedClone);
            handleCloseModal();
            return;
        }
        const checkExist = selectedList.some((s) => s.exerciseTypeId === newData.exerciseTypeId);
        if (checkExist) {
            const indexOfExist = selectedList.findIndex((s) => s.exerciseTypeId === newData.exerciseTypeId);
            const newList = selectedList.map((s) => {
                if (s.exerciseTypeId === newData.exerciseTypeId) {
                    s.exerciseIds = newData.exerciseIds;
                    const newEIds = exercises.filter((e) => newData.exerciseIds.includes(e.id));
                    const newListClone = selectedClone.map((s, index) => {
                        if (index === indexOfExist) {
                            s.lsExercise = newEIds;
                            return s;
                        } else {
                            return s;
                        }
                    });
                    setSelectedClone(newListClone);
                    return s;
                } else {
                    return s;
                }
            });
            setSelectedList(newList);
        } else {
            const eType = exerciseTypes.find((e) => e.id === newData.exerciseTypeId);
            const lsExercise = exercises.filter((e) => newData.exerciseIds.includes(e.id));
            setSelectedClone((prev) => ([...prev, { eType, lsExercise }]));
            setSelectedList((prev) => ([...prev, newData]));
        }

        // setNewData({
        //     exerciseTypeId: 0,
        //     exerciseIds: []
        // });
        handleCloseModal();
    };

    const handleCheckEmpty = () => {
        const isExist = selectedList?.find((s) => s.exerciseTypeId === newData?.exerciseTypeId);
        const checkSelected = selectedList.length === 0 ? true : !isExist ? true : isExist?.exerciseIds?.length === 0;
        const checkCurrent = newData.exerciseIds.length === 0;
        return checkSelected && checkCurrent;
    };

    return (
        <Dialog open={openModal} onClose={handleCloseModal} maxWidth="sm" fullWidth>
            <DialogTitle textAlign={'center'} variant='h5'>Thêm loại bài tập và bài tập</DialogTitle>
            <Divider />
            <DialogContent>
                <Stack spacing={3} mt={2}>
                    <Box>
                        <Typography variant="h6" mb={1}>Loại bài tập:</Typography>
                        <Autocomplete
                            options={exerciseTypes}
                            getOptionLabel={(option) => option.exerciseTypeName}
                            value={exerciseTypes.find(type => type.id === newData.exerciseTypeId) || null}
                            onChange={(event, selected) => {
                                handleChangeData({
                                    target: {
                                        name: 'exerciseTypeId',
                                        value: selected ? selected.id : ''
                                    }
                                });
                            }}
                            renderInput={(params) => (
                                <TextField {...params} placeholder="Chọn loại bài tập" />
                            )}
                        />
                    </Box>

                    <Box>
                        <Typography variant="h6" mb={1}>Bài tập:</Typography>
                        {exercises.length === 0 ? 'Hiện không có bài tập nào!' : exercises.map(exercise => (
                            <Stack key={exercise.id} direction="row" alignItems="center">
                                <Checkbox
                                    name="exerciseIds"
                                    value={exercise.id}
                                    checked={newData.exerciseIds.includes(exercise.id)}
                                    onChange={handleChangeData}
                                />
                                <Typography>{exercise.exerciseName}</Typography>
                            </Stack>
                        ))}
                    </Box>
                </Stack>
            </DialogContent>
            <DialogActions>
                <Button onClick={handleCloseModal} variant='outlined' color="primary">Hủy</Button>
                <Button color="primary" variant="contained" onClick={handleAdd} disabled={handleCheckEmpty()}>Thêm</Button>
            </DialogActions>
        </Dialog>
    );
}

export default ExerciseAdd;
